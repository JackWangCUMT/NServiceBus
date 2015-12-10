﻿namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class EndpointInstancesTests
    {
        [Test]
        public void Should_throw_when_trying_to_configure_instances_that_don_not_match_endpoint_name()
        {
            var instances = new EndpointInstances();
            TestDelegate action = () => instances.AddStatic(new Endpoint("Sales"), new EndpointInstance(new Endpoint("A"), null, null));
            Assert.Throws<ArgumentException>(action);
        }

        [Test]
        public async Task Should_return_instances_configured_by_static_route()
        {
            var instances = new EndpointInstances();
            var sales = new Endpoint("Sales");
            instances.AddStatic(sales, new EndpointInstance(sales, "1", null), new EndpointInstance(sales, "2", null));

            var salesInstances = (await instances.FindInstances(sales)).ToList();
            Assert.AreEqual(2, salesInstances.Count);
        }

        [Test]
        public async Task Should_filter_out_duplicate_instances()
        {
            var instances = new EndpointInstances();
            var sales = new Endpoint("Sales");
            instances.AddStatic(sales, new EndpointInstance(sales, "dup", null), new EndpointInstance(sales, "dup", null));

            var salesInstances = (await instances.FindInstances(sales)).ToList();
            Assert.AreEqual(1, salesInstances.Count);
        }

        [Test]
        public async Task Should_default_to_single_instance_when_not_configured()
        {
            var instances = new EndpointInstances();
            var salesInstances = (await instances.FindInstances(new Endpoint("Sales"))).ToArray();
            Assert.AreEqual(1, salesInstances.Length);
            Assert.IsNull(salesInstances[0].UserDiscriminator);
            Assert.IsNull(salesInstances[0].TransportDiscriminator);
        }
    }
}