﻿namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class TransportAddressesTest
    {
        [Test]
        public void Special_cases_should_override_rules()
        {
            var addresses = new TransportAddresses();
            addresses.AddRule(i => "Rule");
            addresses.AddSpecialCase(new EndpointInstanceName(new EndpointName("Sales"), null, null), "SpecialCase");

            Assert.AreEqual("SpecialCase", addresses.GetTransportAddress(new EndpointInstanceName(new EndpointName("Sales"), null, null)));
            Assert.AreEqual("Rule", addresses.GetTransportAddress(new EndpointInstanceName(new EndpointName("Billing"), null, null)));
        }

        [Test]
        public void Rules_should_override_transport_defaults()
        {
            var addresses = new TransportAddresses();
            addresses.AddRule(i => i.EndpointName.ToString().StartsWith("S") ? "Rule" : null);
            addresses.RegisterTransportDefault(i => "TransportDefault");

            Assert.AreEqual("Rule", addresses.GetTransportAddress(new EndpointInstanceName(new EndpointName("Sales"), null, null)));
            Assert.AreEqual("TransportDefault", addresses.GetTransportAddress(new EndpointInstanceName(new EndpointName("Billing"), null, null)));
        }

        [Test]
        public void It_should_throw_when_rules_are_ambiguous()
        {
            var addresses = new TransportAddresses();
            addresses.AddRule(i => i.EndpointName.ToString().StartsWith("S") ? "Rule1" : null);
            addresses.AddRule(i => i.EndpointName.ToString().EndsWith("s") ? "Rule2" : null);

            TestDelegate action = () => addresses.GetTransportAddress(new EndpointInstanceName(new EndpointName("Sales"), null, null));
            Assert.Throws<Exception>(action);
        }
    }
}