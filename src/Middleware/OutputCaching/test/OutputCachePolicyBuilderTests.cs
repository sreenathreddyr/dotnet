// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.OutputCaching.Tests;

public class OutputCachePolicyBuilderTests
{
    [Fact]
    public void BuildPolicy_CreatesDefaultPolicy()
    {
        var builder = new OutputCachePolicyBuilder();
        var policy = builder.Build();

        Assert.Equal(DefaultPolicy.Instance, policy);
    }

    [Fact]
    public async Task BuildPolicy_CreatesExpirePolicy()
    {
        var context = TestUtils.CreateUninitializedContext();
        var duration = 42;

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.Expire(TimeSpan.FromSeconds(duration)).Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Equal(duration, context.ResponseExpirationTimeSpan?.TotalSeconds);
    }

    [Fact]
    public async Task BuildPolicy_CreatesNoStorePolicy()
    {
        var context = TestUtils.CreateUninitializedContext();

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.NoCache().Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.False(context.EnableOutputCaching);
    }

    [Fact]
    public async Task BuildPolicy_AddsCustomPolicy()
    {
        var options = new OutputCacheOptions();
        var name = "MyPolicy";
        var duration = 42;
        options.AddPolicy(name, b => b.Expire(TimeSpan.FromSeconds(duration)));

        var context = TestUtils.CreateUninitializedContext(options: options);

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.AddPolicy(new NamedPolicy(name)).Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Equal(duration, context.ResponseExpirationTimeSpan?.TotalSeconds);
    }

    [Fact]
    public async Task BuildPolicy_CreatesVaryByHeaderPolicy()
    {
        var context = TestUtils.CreateUninitializedContext();
        context.HttpContext.Request.Headers["HeaderA"] = "ValueA";
        context.HttpContext.Request.Headers["HeaderB"] = "ValueB";

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.VaryByHeader("HeaderA", "HeaderC").Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Contains("HeaderA", (IEnumerable<string>)context.CacheVaryByRules.HeaderNames);
        Assert.Contains("HeaderC", (IEnumerable<string>)context.CacheVaryByRules.HeaderNames);
        Assert.DoesNotContain("HeaderB", (IEnumerable<string>)context.CacheVaryByRules.HeaderNames);
    }

    [Fact]
    public async Task BuildPolicy_CreatesVaryByQueryPolicy()
    {
        var context = TestUtils.CreateUninitializedContext();
        context.HttpContext.Request.QueryString = new QueryString("?QueryA=ValueA&QueryB=ValueB");

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.VaryByQuery("QueryA", "QueryC").Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Contains("QueryA", (IEnumerable<string>)context.CacheVaryByRules.QueryKeys);
        Assert.Contains("QueryC", (IEnumerable<string>)context.CacheVaryByRules.QueryKeys);
        Assert.DoesNotContain("QueryB", (IEnumerable<string>)context.CacheVaryByRules.QueryKeys);
    }

    [Fact]
    public async Task BuildPolicy_CreatesVaryByRoutePolicy()
    {
        var context = TestUtils.CreateUninitializedContext();
        context.HttpContext.Request.RouteValues = new Routing.RouteValueDictionary()
        {
            ["RouteA"] = "ValueA",
            ["RouteB"] = 123.456,
        };

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.VaryByRouteValue("RouteA", "RouteC").Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Contains("RouteA", (IEnumerable<string>)context.CacheVaryByRules.RouteValueNames);
        Assert.Contains("RouteC", (IEnumerable<string>)context.CacheVaryByRules.RouteValueNames);
        Assert.DoesNotContain("RouteB", (IEnumerable<string>)context.CacheVaryByRules.RouteValueNames);
    }

    [Fact]
    public async Task BuildPolicy_CreatesVaryByValuePolicy()
    {
        var context = TestUtils.CreateUninitializedContext();

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.VaryByValue(context => new KeyValuePair<string, string>("color", "blue")).Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Equal("blue", context.CacheVaryByRules.VaryByCustom["color"]);
    }

    [Fact]
    public async Task BuildPolicy_CreatesTagPolicy()
    {
        var context = TestUtils.CreateUninitializedContext();

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.Tag("tag1", "tag2").Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Contains("tag1", context.Tags);
        Assert.Contains("tag2", context.Tags);
    }

    [Fact]
    public async Task BuildPolicy_AllowsLocking()
    {
        var context = TestUtils.CreateUninitializedContext();

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.True(context.AllowLocking);
    }

    [Fact]
    public async Task BuildPolicy_EnablesLocking()
    {
        var context = TestUtils.CreateUninitializedContext();

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.AllowLocking(true).Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.True(context.AllowLocking);
    }

    [Fact]
    public async Task BuildPolicy_DisablesLocking()
    {
        var context = TestUtils.CreateUninitializedContext();

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.AllowLocking(false).Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.False(context.AllowLocking);
    }

    [Fact]
    public async Task BuildPolicy_ClearsDefaultPolicy()
    {
        var context = TestUtils.CreateUninitializedContext();

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.Clear().Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.False(context.AllowLocking);
        Assert.False(context.AllowCacheLookup);
        Assert.False(context.AllowCacheStorage);
        Assert.False(context.EnableOutputCaching);
    }

    [Fact]
    public async Task BuildPolicy_DisablesCache()
    {
        var context = TestUtils.CreateUninitializedContext();

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.NoCache().Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.False(context.EnableOutputCaching);
    }

    [Fact]
    public async Task BuildPolicy_EnablesCache()
    {
        var context = TestUtils.CreateUninitializedContext();

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.NoCache().Cache().Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    public async Task BuildPolicy_ChecksWithPredicate(int source, int expected)
    {
        // Each predicate should override the duration from the first base policy
        var options = new OutputCacheOptions();
        options.AddBasePolicy(build => build.Expire(TimeSpan.FromSeconds(1)));
        options.AddBasePolicy(build => build.With(c => source == 1).Expire(TimeSpan.FromSeconds(2)));
        options.AddBasePolicy(build => build.With(c => source == 2).Expire(TimeSpan.FromSeconds(3)));

        var context = TestUtils.CreateUninitializedContext(options: options);

        foreach (var policy in options.BasePolicies)
        {
            await policy.CacheRequestAsync(context, default);
        }

        Assert.True(context.EnableOutputCaching);
        Assert.Equal(expected, context.ResponseExpirationTimeSpan?.TotalSeconds);
    }

    [Fact]
    public async Task BuildPolicy_NoDefaultWithFalsePredicate()
    {
        // Each predicate should override the duration from the first base policy
        var options = new OutputCacheOptions();
        options.AddBasePolicy(build => build.With(c => false).Expire(TimeSpan.FromSeconds(2)));

        var context = TestUtils.CreateUninitializedContext(options: options);

        foreach (var policy in options.BasePolicies)
        {
            await policy.CacheRequestAsync(context, default);
        }

        Assert.False(context.EnableOutputCaching);
        Assert.NotEqual(2, context.ResponseExpirationTimeSpan?.TotalSeconds);
    }

    [Fact]
    public async Task BuildPolicy_CacheReturnsDefault()
    {
        // Each predicate should override the duration from the first base policy
        var options = new OutputCacheOptions();
        options.AddBasePolicy(build => build.Cache());

        var context = TestUtils.CreateUninitializedContext(options: options);

        foreach (var policy in options.BasePolicies)
        {
            await policy.CacheRequestAsync(context, default);
        }

        Assert.True(context.EnableOutputCaching);
    }
}
