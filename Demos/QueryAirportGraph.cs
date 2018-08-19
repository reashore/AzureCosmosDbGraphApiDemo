using Gremlin.Net.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CosmosDb.GremlinApi.Demos
{
	public static class QueryAirportGraph
	{
		private const int Port = 443;
		private const string DatabaseName = "GraphDb";
		private const string GraphName = "Airport";

		public static async Task Run()
		{
			Debugger.Break();

			string hostname = ConfigurationManager.AppSettings["CosmosDbHostName"];
			string masterKey = ConfigurationManager.AppSettings["CosmosDbMasterKey"];
			string username = $"/dbs/{DatabaseName}/colls/{GraphName}";

			GremlinServer gremlinServer = new GremlinServer(hostname, Port, true, username, masterKey);

			using (GremlinClient client = new GremlinClient(gremlinServer))
			{
				Console.WriteLine();
				Console.WriteLine("*** Scenario 1 - First eat (> .3 rating), then switch terminals, then go to gate ***");

				const string firstEatThenSwitchTerminals = @"
					// Start at T1, Gate 2
						g.V('Gate T1-2')

					// Traverse edge from gate to restaurants
						.outE('gateToRestaurant')
						.inV()

					// Filter for restaurants with a rating higher than .3
						.has('rating', gt(0.3))

					// Traverse edge from restaurant back to terminal (T1)
						.outE('restaurantToTerminal')
						.inV()
					
					// Traverse edge from terminal to next terminal (T2)
						.outE('terminalToNextTerminal')
						.inV()
					
					// Traverse edge from terminal (T2) to gates
						.outE('terminalToGate')
						.inV()
					
					// Filter for destination gate T2, Gate 3
						.has('id', 'Gate T2-3')
					
					// Show the possible paths
						.path()";

				await RunAirportQuery(client, firstEatThenSwitchTerminals);

				Console.WriteLine();
				Console.WriteLine("*** Scenario 2 - First switch terminals, then eat (> .2 rating), then go to gate ***");

				const string firstSwitchTerminalsThenEat = @"
					// Start at T1, Gate 2
						g.V('Gate T1-2')

					// Traverse edge from gate to terminal T1
						.outE('gateToTerminal')
						.inV()

					// Traverse edge from terminal to next terminal (T2)
						.outE('terminalToNextTerminal')
						.inV()

					// Traverse edge from terminal to restaurants
						.outE('terminalToRestaurant')
						.inV()
					
					// Filter for restaurants with a rating higher than .2
						.has('rating', gt(0.2))
					
					// Traverse edge from restaurant back to gates
						.outE('restaurantToGate')
						.inV()
					
					// Filter for destination gate T2, Gate 3
						.has('id', 'Gate T2-3')
					
					// Show the possible paths
						.path()";

				await RunAirportQuery(client, firstSwitchTerminalsThenEat);
			}
		}

		private static async Task RunAirportQuery(IGremlinClient client, string gremlinCode)
		{
			IReadOnlyCollection<dynamic> results = await client.SubmitAsync<dynamic>(gremlinCode);

			int count = 0;

			foreach (dynamic result in results)
			{
				count++;
				dynamic jResult = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(result));
				JArray steps = (JArray)jResult["objects"];

				int userStep = 0;
				int totalDistanceInMinutes = 0;
				int i = 0;

				Console.WriteLine();
				Console.WriteLine($"Choice # {count}");

				foreach (JToken step in steps)
				{
					i++;
					if (step["type"].Value<string>() == "vertex")
					{
						userStep++;
						string userStepCaption = (userStep == 1 ? "Start at" : (i == steps.Count ? "Arrive at" : "Go to"));
						string vertexInfo = $"{userStep}. {userStepCaption} {step["label"]} = {step["id"]}";

						if (step["label"].Value<string>() == "restaurant")
						{
							vertexInfo += $", rating = {step["properties"]["rating"][0]["value"]}";
							vertexInfo += $", avg price = {step["properties"]["averagePrice"][0]["value"]}";
						}

						vertexInfo += $" ({totalDistanceInMinutes} min)";
						Console.WriteLine(vertexInfo);
					}
					else
					{
						int distanceInMinutes = step["properties"]["distanceInMinutes"].Value<int>();
						totalDistanceInMinutes += distanceInMinutes;
						string edgeInfo = $"    ({step["label"]} = {distanceInMinutes} min)";
						Console.WriteLine(edgeInfo);
					}
				}
			}
		}
	}
}
