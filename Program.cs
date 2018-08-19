using CosmosDb.GremlinApi.Demos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CosmosDb.GremlinApi
{
	public static class Program
	{
		private static IDictionary<string, Func<Task>> _demoMethods;

		//public static object PopulateAirportGraphAlt { get; }

		private static void Main()
		{
			_demoMethods = new Dictionary<string, Func<Task>>
			{
				{ "PA", PopulateAirportGraph.Run },
				{ "QA", QueryAirportGraph.Run },
			};

			Task.Run(async () =>
			{
				ShowMenu();
				while (true)
				{
					Console.Write("Selection: ");
					string input = Console.ReadLine();
				    if (string.IsNullOrEmpty(input))
				    {
				        continue;
				    }

				    string demoId = input.ToUpper().Trim();

					if (_demoMethods.Keys.Contains(demoId))
					{
						Func<Task> demoMethod = _demoMethods[demoId];
						await RunDemo(demoMethod);
					}
					else if (demoId == "Q")
					{
						break;
					}
					else
					{
						Console.WriteLine($"?{input}");
					}
				}
			}).Wait();
		}

		private static void ShowMenu()
		{
		    Console.WriteLine(@"Cosmos DB Gremlin .NET Demos:");
		    Console.WriteLine(@"PA Populate airport graph");
		    Console.WriteLine(@"QA Query airport graph");
		    Console.WriteLine(@"Q  Quit");
		}

		private static async Task RunDemo(Func<Task> demoMethod)
		{
			try
			{
				await demoMethod();
			}
			catch (Exception exception)
			{
				string message = exception.Message;

				while (exception.InnerException != null)
				{
					exception = exception.InnerException;
					message += Environment.NewLine + exception.Message;
				}

				Console.WriteLine($"Exception message: {message}");
			}
			Console.WriteLine();
			Console.Write("Done. Press any key to continue...");
			Console.ReadKey(true);
			Console.Clear();
			ShowMenu();
		}
	}
}
