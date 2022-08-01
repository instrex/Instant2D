using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Utils {
    /// <summary>
    /// Simple event bus capable of raising typed events which you can subscribe for.
    /// </summary>
    public class EventBus {
        interface IEvent { }

        // simple event type
        record Event<T> : IEvent {
            public List<Action<T>> Subscribers = new();
        }

        // event type that will stop execution when one of the subscribers return true
        record HandledEvent<T> : IEvent {
            public List<Func<T, bool>> Subscribers = new();
        }

        readonly Dictionary<Type, IEvent> _events = new();

        /// <summary>
        /// Raise all events which accept <paramref name="eventData"/> as parameter.
        /// </summary>
        public void Raise<T>(T eventData) {
            if (!_events.TryGetValue(typeof(T), out var ev)) {
                throw new InvalidOperationException($"Attempted to raise unknown event type: {typeof(T).Name}");
            }

            switch (ev) {
                default: throw new InvalidOperationException($"Invalid event data: {typeof(T).Name}");

                case Event<T> { Subscribers: var subscribers }:
                    foreach (var sub in subscribers) {
                        sub?.Invoke(eventData);
                    }

                    break;

                case HandledEvent<T> { Subscribers: var handlers }:
                    foreach (var handler in handlers) {
                        // stop after first successful handler
                        if (handler?.Invoke(eventData) == true)
                            break;
                    }

                    break;
            }
        }

        /// <summary>
        /// Add new subscription to an event with type <typeparamref name="T"/>.
        /// </summary>
        public void Subscribe<T>(Action<T> action) {
            Event<T> ev = _events.TryGetValue(typeof(T), out var foundEvent) ?
                foundEvent as Event<T> : new Event<T>();
            ev.Subscribers.Add(action);

            // register new event
            if (foundEvent is null) {
                _events.Add(typeof(T), ev);
            }
        }

        /// <summary>
        /// Add new handler to an event with type <typeparamref name="T"/>.
        /// </summary>
        public void Subscribe<T>(Func<T, bool> action) {
            HandledEvent<T> ev = _events.TryGetValue(typeof(T), out var foundEvent) ?
                foundEvent as HandledEvent<T> : new HandledEvent<T>();
            ev.Subscribers.Add(action);

            // register new event
            if (foundEvent is null) {
                _events.Add(typeof(T), ev);
            }
        }

        /// <summary>
        /// Remove the event subscription.
        /// </summary>
        public void Unsubscribe<T>(Action<T> action) {
            if (_events.TryGetValue(typeof(T), out var foundEvent) && foundEvent is Event<T> ev) {
                ev.Subscribers.Remove(action);
            }
        }

        /// <summary>
        /// Remove the handler subscription.
        /// </summary>
        public void Unsubscribe<T>(Func<T, bool> action) {
            if (_events.TryGetValue(typeof(T), out var foundEvent) && foundEvent is HandledEvent<T> ev) {
                ev.Subscribers.Remove(action);
            }
        }
    }
}
