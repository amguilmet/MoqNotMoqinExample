using System.Reflection.Metadata;
using Moq;

namespace MoqVerifyNotWorking
{
    public class Event
    {
        public readonly Guid CorrelationId;

        public Event()
        {
            CorrelationId = Guid.NewGuid();
        }
    }

    public class EventToVerify : Event
    {
        public readonly string Data;
        public EventToVerify(string data)
        {
            Data = data;
        }
    }

    public interface IEventPublisher
    {
        public void PublishEvent<T>(T ev) where T: Event;
    }

    public class MessageBus : IEventPublisher
    {
        public virtual void PublishEvent<T>(T ev) where T : Event 
        {
            Console.WriteLine("------------------Event WAS published: " + ev.CorrelationId);
        }
    }

    public class Entity
    {
        private IList<Event> _events;

        public string Data { get; private set; }

        public Entity()
        {
            Data = "Nothing";
            _events = new List<Event>();
        }

        public void ApplyEvent(EventToVerify ev)
        {
            Data = ev.Data;

            _events.Add(ev);
        }

        public IList<Event> GetEvents()
        {
            return _events;
        }
    }

    public class Repository
    {
        private IEventPublisher _publisher;
        public Repository(IEventPublisher publisher)
        {
            _publisher = publisher;
        }

        public void Save(Entity e)
        {
            //persistence logic here

            foreach (var ev in e.GetEvents())
            {
                _publisher.PublishEvent(ev);
            }
        }
    }


    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void VerifyEventWasPublished()
        {
            var mockBus = new Mock<MessageBus>();
            var realBus = new MessageBus();

            var repoWithMock = new Repository(mockBus.Object);
            var repoWithReal = new Repository(realBus);

            Entity entWithReal = new Entity();
            entWithReal.ApplyEvent(new EventToVerify("New Data"));
            repoWithReal.Save(entWithReal);

            mockBus.Setup(e=>e.PublishEvent(It.IsAny<EventToVerify>())).Verifiable();
            Entity entWithMock = new Entity();
            entWithMock.ApplyEvent(new EventToVerify("New Data"));
            repoWithMock.Save(entWithMock);
            mockBus.Verify(e=>e.PublishEvent(It.IsAny<EventToVerify>()));
        }
    }
}