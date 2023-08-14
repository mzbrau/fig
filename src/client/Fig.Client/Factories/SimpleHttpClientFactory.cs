using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using Fig.Common.NetStandard.Constants;

namespace Fig.Client.Factories;

public class SimpleHttpClientFactory : IHttpClientFactory
{
    private readonly ReadOnlyDictionary<string, HttpClient> _clients;

    public SimpleHttpClientFactory(Uri baseAddress)
    {
        _clients = new ReadOnlyDictionary<string, HttpClient>(new Dictionary<string, HttpClient>()
        {
            {
                HttpClientNames.FigApi, new HttpClient()
                {
                    BaseAddress = baseAddress
                }
            }
        });
    }
    
    public SimpleHttpClientFactory(Dictionary<string, HttpClient> clients)
    {
        _clients = new ReadOnlyDictionary<string, HttpClient>(clients);
    }

    public HttpClient CreateClient(string name)
    {
        if (_clients.TryGetValue(name, out var client))
        {
            return client;
        }

        throw new KeyNotFoundException($"No HttpClient registered with name {name}");
    }
}