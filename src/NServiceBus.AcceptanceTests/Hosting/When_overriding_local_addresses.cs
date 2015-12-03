namespace NServiceBus.AcceptanceTests.Hosting
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_overriding_local_addresses : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_custom_queue_names()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<MyEndpoint>(e => e.When(b => b.SendLocal(new MyMessage())))
                .Done(c => c.Done)
                .Run();

            Assert.IsTrue(context.Done);
            Assert.IsTrue(context.InputQueue.StartsWith("OverriddenLocalAddress"));
        }

        public class MyEndpoint : EndpointConfigurationBuilder
        {
            public MyEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableFeature<TimeoutManager>();
                    var transportExtensions = new TransportExtensions(c.GetSettings());
                    transportExtensions.AddAddressTranslationRule(address =>
                    {
                        return "OverriddenLocalAddress" + address.Qualifier; //Overriding -> Overridden
                    });
                });
            }
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                Context.Done = true;
                Context.InputQueue = context.MessageHeaders[Headers.ReplyToAddress];

                return Task.FromResult(0);
            }
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public string InputQueue { get; set; }
        }

        [Serializable]
        public class MyMessage : ICommand
        {
        }
        
    }
}