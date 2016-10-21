﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Microsoft.eShopOnContainers.Services.Basket.API.Model
{
    public class RedisBasketRepository : IBasketRepository
    {
        private ILogger<RedisBasketRepository> _logger;
        private BasketSettings _settings;

        private ConnectionMultiplexer _redis;


        public RedisBasketRepository(IOptions<BasketSettings> options, ILoggerFactory loggerFactory)
        {
            _settings = options.Value;
            _logger = loggerFactory.CreateLogger<RedisBasketRepository>();

        }

        public async Task<bool> DeleteBasket(Guid id)
        {
            var database = await GetDatabase();
            return await database.KeyDeleteAsync(id.ToString());
        }

        public async Task<CustomerBasket> GetBasket(Guid customerId)
        {
            var database = await GetDatabase();

            var data = await database.StringGetAsync(customerId.ToString());
            if (data.IsNullOrEmpty)
            {
                return null;
            }

            return JsonConvert.DeserializeObject<CustomerBasket>(data);
        }

        public async Task<bool> UpdateBasket(CustomerBasket basket)
        {
            var database = await GetDatabase();
            return await database.StringSetAsync(basket.CustomerId.ToString(), JsonConvert.SerializeObject(basket));
        }

        private async Task<IDatabase> GetDatabase()
        {
            if (_redis == null)
            {
                //TODO: Need to make this more robust. Also want to understand why the static connection method cannot accept dns names.
                var ips = await Dns.GetHostAddressesAsync(_settings.ConnectionString);
                _logger.LogInformation($"Connecting to database {_settings.ConnectionString} at IP {ips.First().ToString()}");
                _redis = await ConnectionMultiplexer.ConnectAsync(ips.First().ToString());
            }

            return _redis.GetDatabase();
        }
    }
}