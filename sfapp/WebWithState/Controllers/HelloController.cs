using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using System.Fabric;
using System.Threading;

namespace WebWithState.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class HelloController : Controller
    {

        private static readonly Uri DICO_NAME = new Uri("store:/hellos");

        private readonly IReliableStateManager _stateManager;

        public HelloController(IReliableStateManager stateManager)
        {
            _stateManager = stateManager;
        }

        // GET: api/hello/name
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var result = new List<KeyValuePair<string, int>>();

                var tryGetResult = await _stateManager.TryGetAsync<IReliableDictionary<string, int>>(DICO_NAME);

                if (tryGetResult.HasValue)
                {
                    var dictionary = tryGetResult.Value;

                    using (var tx = _stateManager.CreateTransaction())
                    {
                        var enumerable = await dictionary.CreateEnumerableAsync(tx);
                        var enumerator = enumerable.GetAsyncEnumerator();

                        while (await enumerator.MoveNextAsync(CancellationToken.None))
                        {
                            result.Add(enumerator.Current);
                        }
                    }
                }
                return this.Json(result);
            }
            catch (FabricException)
            {
                return new ContentResult { StatusCode = 503, Content = "The service was unable to process the request. Please try again." };
            }
        }

        // PUT: api/hello/name
        [HttpPut("{name}")]
        public async Task<IActionResult> PutAsync(string name)
        {
            try
            {
                var dictionary = await _stateManager.GetOrAddAsync<IReliableDictionary<string, int>>(DICO_NAME);

                using (ITransaction tx = _stateManager.CreateTransaction())
                {
                    await dictionary.AddOrUpdateAsync(tx, name, 1, (key, oldValue)=>oldValue + 1);
                    await tx.CommitAsync();
                }

                return this.Ok();
            }
            catch (FabricNotPrimaryException)
            {
                return new ContentResult { StatusCode = 410, Content = "The primary replica has moved. Please re-resolve the service." };
            }
            catch (FabricException)
            {
                return new ContentResult { StatusCode = 503, Content = "The service was unable to process the request. Please try again." };
            }
        }        
    }
}
