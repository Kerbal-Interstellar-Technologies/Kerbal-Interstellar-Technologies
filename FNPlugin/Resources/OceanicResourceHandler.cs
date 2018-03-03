﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Resources
{
	class OceanicResourceHandler
	{
		protected static Dictionary<int, List<OceanicResource>> body_oceanic_resource_list = new Dictionary<int, List<OceanicResource>>();

		public static double getOceanicResourceContent(int refBody, string resourcename)
		{
			List<OceanicResource> bodyOceanicComposition = GetOceanicCompositionForBody(refBody);
			OceanicResource resource = bodyOceanicComposition.FirstOrDefault(oor => oor.ResourceName == resourcename);
			return resource != null ? resource.ResourceAbundance : 0;
		}

		public static double getOceanicResourceContent(int refBody, int resource)
		{
			List<OceanicResource> bodyOceanicComposition = GetOceanicCompositionForBody(refBody);
			if (bodyOceanicComposition.Count > resource) return bodyOceanicComposition[resource].ResourceAbundance;
			return 0;
		}

		public static string getOceanicResourceName(int refBody, int resource)
		{
			List<OceanicResource> bodyOceanicComposition = GetOceanicCompositionForBody(refBody);
			if (bodyOceanicComposition.Count > resource)
			{
				return bodyOceanicComposition[resource].ResourceName;
			}
			return null;
		}

		public static string getOceanicResourceDisplayName(int refBody, int resource)
		{
			List<OceanicResource> bodyOceanicComposition = GetOceanicCompositionForBody(refBody);
			if (bodyOceanicComposition.Count > resource)
			{
				return bodyOceanicComposition[resource].DisplayName;
			}
			return null;
		}

		public static List<OceanicResource> GetOceanicCompositionForBody(CelestialBody celestialBody) // getter that uses celestial body as an argument
		{
			return GetOceanicCompositionForBody(celestialBody.flightGlobalsIndex); // calls the function that uses refBody int as an argument
		}

		public static List<OceanicResource> GetOceanicCompositionForBody(int refBody) // function for getting or creating oceanic composition
		{
			List<OceanicResource> bodyOceanicComposition = new List<OceanicResource>(); // create an object list for holding all the resources
			try
			{
				// check if there's a composition for this body
				if (body_oceanic_resource_list.ContainsKey(refBody)) 
				{
					// skip all the other stuff and return the composition we already have
					return body_oceanic_resource_list[refBody]; 
				}
				else
				{
					CelestialBody celestialBody = FlightGlobals.Bodies[refBody]; // create a celestialBody object referencing the current body (makes it easier on us in the next lines)

					// create composition from kspi oceanic definition file
					bodyOceanicComposition = CreateFromKspiOceanDefinitionFile(refBody, celestialBody);

					// add from stock resource definitions if missing
					GenerateCompositionFromResourceAbundances(refBody, bodyOceanicComposition); // calls the generating function below

					// if no ocean definition is created, create one based on celestialBody characteristics
					if (bodyOceanicComposition.Sum(m => m.ResourceAbundance) < 0.5)
						bodyOceanicComposition = GenerateCompositionFromCelestialBody(celestialBody);

					// Add rare and isotopic resources
					AddRaresAndIsotopesToOceanComposition(bodyOceanicComposition);

					// add missing stock resources
					AddMissingStockResources(refBody, bodyOceanicComposition);

					// sort on resource abundance
					bodyOceanicComposition = bodyOceanicComposition.OrderByDescending(bacd => bacd.ResourceAbundance).ToList();

					// add to database for future reference
					body_oceanic_resource_list.Add(refBody, bodyOceanicComposition);
				}
			}
			catch (Exception ex)
			{
				Debug.Log("[KSPI] - Exception while loading oceanic resources : " + ex.ToString());
			}
			return bodyOceanicComposition;
		}

		private static List<OceanicResource> CreateFromKspiOceanDefinitionFile(int refBody, CelestialBody celestialBody)
		{
			var bodyOceanicComposition = new List<OceanicResource>();

			ConfigNode oceanic_resource_pack = GameDatabase.Instance.GetConfigNodes("OCEANIC_RESOURCE_PACK_DEFINITION_KSPI").FirstOrDefault();

			Debug.Log("[KSPI] Loading oceanic data from pack: " + (oceanic_resource_pack.HasValue("name") ? oceanic_resource_pack.GetValue("name") : "unknown pack"));
			if (oceanic_resource_pack != null)
			{
				Debug.Log("[KSPI] - searching for ocean definition for " + celestialBody.name);
				List<ConfigNode> oceanic_resource_list = oceanic_resource_pack.nodes.Cast<ConfigNode>().Where(res => res.GetValue("celestialBodyName") == FlightGlobals.Bodies[refBody].name).ToList();
				if (oceanic_resource_list.Any())
					bodyOceanicComposition = oceanic_resource_list.Select(orsc => new OceanicResource(orsc.HasValue("resourceName") ? orsc.GetValue("resourceName") : null, double.Parse(orsc.GetValue("abundance")), orsc.GetValue("guiName"))).ToList();
			}
			return bodyOceanicComposition;
		}

		public static List<OceanicResource> GenerateCompositionFromCelestialBody(CelestialBody celestialBody) // generates oceanic composition based on planetary characteristics
		{
			List<OceanicResource> bodyOceanicComposition = new List<OceanicResource>(); // instantiate a new list that this function will be returning

			// return empty if there's no ocean
			if (!celestialBody.ocean)
				return bodyOceanicComposition;

			try
			{
				// Lookup homeworld
				CelestialBody homeworld = FlightGlobals.Bodies.SingleOrDefault(b => b.isHomeWorld);

				double pressureAtSurface = celestialBody.GetPressure(0);

				if (celestialBody.Mass < (homeworld.Mass / 1000) && pressureAtSurface > 0 && pressureAtSurface < 10) // is it tiny and has only trace atmosphere?
				{
					// it is similar to Enceladus, use that as a template
					bodyOceanicComposition = GetOceanicCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Enceladus").flightGlobalsIndex);
				}
				else if (celestialBody.Mass < (homeworld.Mass / 100) && pressureAtSurface > 0 && pressureAtSurface < 100) // is it still tiny, but a bit larger and has thin atmosphere?
				{
					// it is Europa-like, use Europa as a template
					bodyOceanicComposition = GetOceanicCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Europa").flightGlobalsIndex);
				}
				else if (celestialBody.Mass < (homeworld.Mass / 40) && pressureAtSurface > 0 && pressureAtSurface < 10 && celestialBody.atmosphereContainsOxygen) // if it is significantly smaller than the homeworld and has trace atmosphere with oxygen
				{
					// it is Ganymede-like, use Ganymede as a template
					bodyOceanicComposition = GetOceanicCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Ganymede").flightGlobalsIndex);
				}
				else if (celestialBody.Mass < homeworld.Mass && pressureAtSurface > 140) // if it is smaller than the homeworld and has pressure at least 140kPA
				{
					// it is Titan-like, use Titan as a template
					bodyOceanicComposition = GetOceanicCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Titan").flightGlobalsIndex);
				}
				else if (pressureAtSurface > 200)
				{
					// it is Venus-like/Eve-like, use Eve as a template
					bodyOceanicComposition = GetOceanicCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Eve").flightGlobalsIndex);
				}
				else if (celestialBody.Mass > (homeworld.Mass / 2) && celestialBody.Mass < homeworld.Mass && pressureAtSurface < 100) // it's at least half as big as the homeworld and has significant atmosphere
				{
					// it is Laythe-like, use Laythe as a template
					bodyOceanicComposition = GetOceanicCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Laythe").flightGlobalsIndex);
				}
				else if (celestialBody.atmosphereContainsOxygen)
				{
					// it is Earth-like, use Earth as a template
					bodyOceanicComposition = GetOceanicCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Earth").flightGlobalsIndex);
				}
				else
				{
					// nothing yet
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("[KSPI] - Exception while generating oceanic composition from celestial ocean properties : " + ex.ToString());
			}

			return bodyOceanicComposition;
		}

		public static List<OceanicResource> GenerateCompositionFromResourceAbundances(int refBody, List<OceanicResource> bodyComposition)
		{
			try
			{
				AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.Ammonia, "LqdAmmonia", "NH3", "Ammonia", "Ammonia");
				AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.Argon, "LqdArgon", "ArgonGas", "Argon", "Argon");
				AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.CarbonDioxide, "LqdCO2", "CO2", "CarbonDioxide", "CarbonDioxide");
				AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.CarbonMoxoxide, "LqdCO", "CO", "CarbonMonoxide", "CarbonMonoxide");
				AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.DeuteriumGas, "LqdDeuterium", "DeuteriumGas", "Deuterium", "Deuterium");
				AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.HeavyWater, "DeuteriumWater", "D2O", "HeavyWater", "HeavyWater");
				AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.KryptonGas, "LqdKrypton", "KryptonGas", "Krypton", "Krypton");
				AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.Methane, "LqdMethane", "MethaneGas", "Methane", "Methane");
				AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.Nitrogen, "LqdNitrogen", "NitrogenGas", "Nitrogen", "Nitrogen");
				AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.NeonGas, "LqdNeon", "NeonGas", "Neon", "Neon");
				AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.OxygenGas, "LqdOxygen", "OxygenGas", "Oxygen", "Oxygen");
				AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.Sodium, "LqdSodium", "SodiumGas", "Sodium", "Sodium");
				AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.Water, "LqdWater", "H2O", "Water", "Water");
				AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.XenonGas, "LqdXenon", "XenonGas", "Xenon", "Xenon");

				AddResource(InterstellarResourcesConfiguration.Instance.LqdHelium4, "Helium-4", refBody, bodyComposition, new[] { "LqdHe4", "Helium4Gas", "Helium4", "Helium-4", "He4Gas", "He4", "LqdHelium", "Helium", "HeliumGas" });
				AddResource(InterstellarResourcesConfiguration.Instance.LqdHelium3, "Helium-3", refBody, bodyComposition, new[] { "LqdHe3", "Helium3Gas", "Helium3", "Helium-3", "He3Gas", "He3" });
				AddResource(InterstellarResourcesConfiguration.Instance.Hydrogen, "Hydrogen", refBody, bodyComposition, new[] { "LqdHydrogen", "HydrogenGas", "Hydrogen", "H2", "Protium", "LqdProtium"});
			}
			catch (Exception ex)
			{
				Debug.LogError("[KSPI] - Exception while generating oceanic composition from defined abundances : " + ex.ToString());
			}

			return bodyComposition;
		}

		private static void AddMissingStockResources(int refBody, List<OceanicResource> bodyComposition)
		{
			// fetch all oceanic resources
			var allOceanicResources = ResourceMap.Instance.FetchAllResourceNames(HarvestTypes.Oceanic);

			Debug.Log("[KSPI] - AddMissingStockResources : found " + allOceanicResources.Count + " resources");

			foreach (var resoureName in allOceanicResources)
			{
				// add resource if missing
				AddMissingResource(resoureName, refBody, bodyComposition);
			}
		}

		private static void AddMissingResource(string resourname, int refBody, List<OceanicResource> bodyComposition)
		{
			// verify it is a defined resource
			PartResourceDefinition definition = PartResourceLibrary.Instance.GetDefinition(resourname);
			if (definition == null)
			{
				Debug.LogWarning("[KSPI] - AddMissingResource : Failed to find resource definition for '" + resourname + "'");
				return;
			}

			// skip it already registred or used as a Synonym
			if (bodyComposition.Any(m => m.ResourceName == definition.name || m.DisplayName == definition.displayName || m.Synonyms.Contains(definition.name)))
			{
				Debug.Log("[KSPI] - AddMissingResource : Already found existing composition for '" + resourname + "'");
				return;
			}

			// retreive abundance
			var abundance = GetAbundance(definition.name, refBody);
			if (abundance <= 0)
			{
				Debug.LogWarning("[KSPI] - AddMissingResource : Abundance for resource '" + resourname + "' was " + abundance);
				return;
			}

			// create oceanicresource from definition and abundance
			var OceanicResource = new OceanicResource(definition, abundance);

			// add to oceanic composition
			Debug.Log("[KSPI] - AddMissingResource : add resource '" + resourname + "'");
			bodyComposition.Add(OceanicResource);
		}

		private static void AddResource(string outputResourname, string displayname, int refBody, List<OceanicResource> bodyOceanicComposition, string[] variants)
		{
			var abundances = new[] { GetAbundance(outputResourname, refBody)}.Concat(variants.Select(m => GetAbundance(m, refBody)));

			var OceanicResource = new OceanicResource(outputResourname, abundances.Max(), displayname, variants);
			if (OceanicResource.ResourceAbundance > 0)
			{
				var existingResource = bodyOceanicComposition.FirstOrDefault(a => a.ResourceName == outputResourname);
				if (existingResource != null)
				{
					Debug.Log("[KSPI] - replaced resource " + outputResourname + " with stock defined abundance " + OceanicResource.ResourceAbundance);
					bodyOceanicComposition.Remove(existingResource);
				}
				bodyOceanicComposition.Add(OceanicResource);
			}
		}

		private static void AddResource(int refBody, List<OceanicResource> bodyOceanicComposition, string outputResourname, string inputResource1, string inputResource2, string inputResource3, string displayname)
		{
			var abundances = new[] { GetAbundance(inputResource1, refBody), GetAbundance(inputResource2, refBody), GetAbundance(inputResource2, refBody) };

			var OceanicResource = new OceanicResource(outputResourname, abundances.Max(), displayname, new[] { inputResource1, inputResource2, inputResource3 });
			if (OceanicResource.ResourceAbundance > 0)
			{
				var existingResource = bodyOceanicComposition.FirstOrDefault(a => a.ResourceName == outputResourname);
				if (existingResource != null)
				{
					Debug.Log("[KSPI] - replaced resource " + outputResourname + " with stock defined abundance " + OceanicResource.ResourceAbundance);
					bodyOceanicComposition.Remove(existingResource);
				}
				bodyOceanicComposition.Add(OceanicResource);
			}
		}

		private static void AddRaresAndIsotopesToOceanComposition(List<OceanicResource> bodyOceanicComposition)
		{
			Debug.Log("[KSPI] - Checking for missing rare isotopes");

			// add heavywater based on water abundance in ocean
			if (bodyOceanicComposition.All(m => m.ResourceName != InterstellarResourcesConfiguration.Instance.HeavyWater) && bodyOceanicComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.Water))
			{
				Debug.Log("[KSPI] - Added heavy water based on presence water in ocean");
				var water = bodyOceanicComposition.First(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.Water);
				var heavywaterAbundance = water.ResourceAbundance / 6420;
				bodyOceanicComposition.Add(new OceanicResource(InterstellarResourcesConfiguration.Instance.HeavyWater, heavywaterAbundance, "HeavyWater", new[] { "HeavyWater", "D2O", "DeuteriumWater"}));
			}
			else
			{
				if (bodyOceanicComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.Water))
					Debug.Log("[KSPI] - No heavy water added because no water found in Ocean");
				else
					Debug.Log("[KSPI] - No heavy water already present in Ocean");
			}

			if (bodyOceanicComposition.All(m => m.ResourceName != InterstellarResourcesConfiguration.Instance.Lithium6) && bodyOceanicComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.Lithium7))
			{
				Debug.Log("[KSPI] - Added lithium-6 based on presence Lithium in ocean");
				var lithium = bodyOceanicComposition.First(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.Lithium7);
				var heavywaterAbundance = lithium.ResourceAbundance * 0.0759;
				bodyOceanicComposition.Add(new OceanicResource(InterstellarResourcesConfiguration.Instance.Lithium6, heavywaterAbundance, "Lithium-6", new[] { "Lithium6", "Lithium-6", "Li6", "Li-6" }));
			}
			else
			{
				if (bodyOceanicComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.Lithium7))
					Debug.Log("[KSPI] - No Lithium-6 added because no Lithium found in Ocean");
				else
					Debug.Log("[KSPI] - No Lithium-6 slready present in Ocean");
			}
		}

		private static float GetAbundance(string resourceName, int refBody)
		{
			return ResourceMap.Instance.GetAbundance(CreateRequest(resourceName, refBody));
		}

		public static AbundanceRequest CreateRequest(string resourceName, int refBody)
		{
			return new AbundanceRequest
			{
				ResourceType = HarvestTypes.Oceanic,
				ResourceName = resourceName,
				BodyId = refBody,
				CheckForLock = false
			};
		}

	}
}
