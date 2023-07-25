using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace skchatgptazurefunction
{
    public class GetPokemon
    {
        public static readonly HttpClient client = new HttpClient();
        private readonly ILogger _logger;
        private readonly IMemoryCache _cache;

        public GetPokemon(ILoggerFactory loggerFactory, IMemoryCache cache)
        {
            _logger = loggerFactory.CreateLogger<GetPokemon>();
            _cache = cache;
        }

        [OpenApiOperation(operationId: "GetPokemonDetails", tags: new[] { "ExecuteFunction" }, Description = "Gets the details for a given pokemon, or a random one if no name or id is provided")]
        [OpenApiParameter(name: "pokemonNameOrId", Description = "The name or id of a pokemon, or an empty string", Required = false, In = ParameterLocation.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The details of either a requested or a random pokemon")]
        [Function("GetPokemonDetails")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            try
            {
                Pokemon pokemon = await GetPokemonDetailsAsync(req.Query["pokemonNameOrId"]);

                HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                response.WriteString(JsonConvert.SerializeObject(pokemon));

                _logger.LogInformation($"Pokemon details were found for {req.Query["pokemonNameOrId"]}");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                HttpResponseData response = req.CreateResponse(HttpStatusCode.BadRequest);
                response.Headers.Add("Content-Type", "text/plain");
                string errorMessage = $"Pokemon details were not found for {req.Query["pokemonNameOrId"] ?? "a random pokemon"}";
                response.WriteString(errorMessage);

                _logger.LogInformation(errorMessage);

                return response;
            }
        }

        public async Task<Pokemon> GetPokemonDetailsAsync(string? pokemonNameOrId)
        {
            // Get a random pokemon if none is specified
            if (string.IsNullOrEmpty(pokemonNameOrId))
            {
                int randomId = new Random().Next(1, 898);
                pokemonNameOrId = randomId.ToString();
            }

            string url = $"https://pokeapi.co/api/v2/pokemon/{pokemonNameOrId.ToLower()}";

            if (_cache.TryGetValue(url, out Pokemon cachedPokemon))
            {
                // Return the cached pokemon if it exists
                return cachedPokemon;
            }

            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetStringAsync(url);
                var pokemon = JsonConvert.DeserializeObject<Pokemon>(response);

                // Cache the pokemon permanently
                _cache.Set(url, pokemon, TimeSpan.FromDays(30));

                return pokemon;
            }
        }

    }

    public class Pokemon
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public int BaseExperience { get; set; }
        public int Height { get; set; }
        public int Id { get; set; }
        public bool IsDefault { get; set; }
        public string LocationAreaEncounters { get; set; }
        public int Order { get; set; }
        public int Weight { get; set; }
        public List<Ability> Abilities { get; set; }
        public List<Form> Forms { get; set; }
        public List<Move> Moves { get; set; }
        public List<Stat> Stats { get; set; }
        public List<PokemonType> Types { get; set; }
    }

    public class Ability
    {
        [JsonProperty("ability")]
        public AbilityDetails AbilityDetails { get; set; }
        public bool IsHidden { get; set; }
        public int Slot { get; set; }
    }

    public class AbilityDetails
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }

    public class Form
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }

    public class Move
    {
        [JsonProperty("move")]
        public MoveDetails MoveDetails { get; set; }
    }

    public class MoveDetails
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }

    public class Stat
    {
        public int BaseStat { get; set; }
        public int Effort { get; set; }
        [JsonProperty("stat")]
        public StatDetails StatDetails { get; set; }
    }

    public class StatDetails
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }

    public class PokemonType
    {
        public int Slot { get; set; }
        public TypeDetails Type { get; set; }
    }

    public class TypeDetails
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }
}
