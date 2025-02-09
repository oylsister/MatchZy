using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CounterStrikeSharp.API;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Newtonsoft.Json.Linq;

namespace MatchZy
{
    public interface IPostRequest
    {

    }

    public class PostRequest : IPostRequest
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MatchZy> _logger;

        private PostRequestConfig? _config;

        public void InitializePostRequest()
        {
            string fileName = "postrequest.json";
            string configFile = Path.Combine(Server.GameDirectory + "/csgo/cfg/MatchZy", fileName);
            if (!File.Exists(configFile))
            {
                // Create a default configuration if the file doesn't exist
                Log($"[InitializePostRequest] database.json doesn't exist, creating default!");
                CreateDefaultConfigFile(configFile);
            }

            _config = JsonSerializer.Deserialize<PostRequestConfig>(File.ReadAllText(configFile)) ?? new();
            _httpClient.BaseAddress = new Uri(_config.PostUri);
        }

        public void CreateDefaultConfigFile(string configFile)
        {
            PostRequestConfig config = new PostRequestConfig
            {
                Enable = false,
                PostUri = ""
            };

            // Serialize and save the default configuration to the file
            string defaultConfigJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configFile, defaultConfigJson);
            Log($"[CreateDefaultConfigFile] Default configuration file created at: {configFile}");
        }

        public long InitMatch(string team1name, string team2name, string serverIp, bool isMatchSetup, long liveMatchId, int mapNumber, string seriesType)
        {
            try
            {
                string mapName = Server.MapName;
                string dateTimeExpression = DateTime.Now.ToString();

                if (mapNumber == 0)
                {
                    if (isMatchSetup && liveMatchId != -1)
                    {
                        /*
                        connection.Execute(@"
                            INSERT INTO matchzy_stats_matches (matchid, start_time, team1_name, team2_name, series_type, server_ip)
                            VALUES (@liveMatchId, " + dateTimeExpression + ", @team1name, @team2name, @seriesType, @serverIp)",
                            new { liveMatchId, team1name, team2name, seriesType, serverIp });
                        */
                        JObject content = new JObject();
                        content["matchid"] = liveMatchId;
                        content["start_time"] = dateTimeExpression;
                        content["team1_name"] = team1name;
                        content["team2_name"] = team2name;
                        content["series_type"] = seriesType;
                        content["server_ip"] = serverIp;

                        var stringContent = new StringContent(content.ToString(), Encoding.UTF8, "application/json");

                        Task.Run(async () =>
                        {
                            var response = await _httpClient.PostAsync(_config?.PostUri, stringContent);

                            if(!response.IsSuccessStatusCode)
                            {
                                _logger.LogCritical("[InitMatch] Post request failed!");
                            }
                        });
                    }
                    else
                    {
                        /*
                        connection.Execute(@"
                            INSERT INTO matchzy_stats_matches (start_time, team1_name, team2_name, series_type, server_ip)
                            VALUES (" + dateTimeExpression + ", @team1name, @team2name, @seriesType, @serverIp)",
                            new { team1name, team2name, seriesType, serverIp });
                        */

                        JObject content = new JObject();
                        content["start_time"] = dateTimeExpression;
                        content["team1_name"] = team1name;
                        content["team2_name"] = team2name;
                        content["series_type"] = seriesType;
                        content["server_ip"] = serverIp;

                        var stringContent = new StringContent(content.ToString(), Encoding.UTF8, "application/json");

                        Task.Run(async () =>
                        {
                            var response = await _httpClient.PostAsync(_config?.PostUri, stringContent);

                            if (!response.IsSuccessStatusCode)
                            {
                                _logger.LogCritical("[InitMatch] Post request failed!");
                            }
                        });
                    }
                }

                if (isMatchSetup && liveMatchId != -1)
                {
                    /*
                    connection.Execute(@"
                        INSERT INTO matchzy_stats_maps (matchid, start_time, mapnumber, mapname)
                        VALUES (@liveMatchId, " + dateTimeExpression + ", @mapNumber, @mapName)",
                        new { liveMatchId, mapNumber, mapName });
                    */

                    JObject content = new JObject();
                    content["matchid"] = liveMatchId;
                    content["start_time"] = dateTimeExpression;
                    content["mapnumber"] = mapNumber;
                    content["mapname"] = mapName;

                    var stringContent = new StringContent(content.ToString(), Encoding.UTF8, "application/json");

                    Task.Run(async () =>
                    {
                        var response = await _httpClient.PostAsync(_config?.PostUri, stringContent);

                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.LogCritical("[InitMatch] Post request failed!");
                        }
                    });

                    return liveMatchId;
                }

                // Retrieve the last inserted match_id
                //long matchId = -1;
                /*
                if (connection is SqliteConnection)
                {
                    matchId = connection.ExecuteScalar<long>("SELECT last_insert_rowid()");
                }
                else if (connection is MySqlConnection)
                {
                    matchId = connection.ExecuteScalar<long>("SELECT LAST_INSERT_ID()");
                }

                connection.Execute(@"
                    INSERT INTO matchzy_stats_maps (matchid, start_time, mapnumber, mapname)
                    VALUES (@matchId, " + dateTimeExpression + ", @mapNumber, @mapName)",
                    new { matchId, mapNumber, mapName });

                Log($"[InsertMatchData] Data inserted into matchzy_stats_matches with match_id: {matchId}");
                return matchId;
                */
            }
            catch (Exception ex)
            {
                Log($"[InsertMatchData - FATAL] Error inserting data: {ex.Message}");
                return liveMatchId;
            }
        }

        private void Log(string message)
        {
            Console.WriteLine("[MatchZy] " + message);
        }
    }

    public class PostRequestConfig
    {
        public bool Enable { get; set; } = false;
        public string PostUri { get; set; } = "";
    }
}
