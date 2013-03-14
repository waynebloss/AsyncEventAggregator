using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AsyncEventAggregator
{
    public sealed class AsyncEventHub
    {
        private const string FailedToGetEventHandlerTaskFactoriesExceptionMessage = @"Failed to get event handler task factories!";
        private const string FailedToGetSubscribersExceptionMessage = @"Failed to get subscribers!";
        private const string FailedToRemoveEventHandlerTaskFactories = @"Failed to remove event handler task factories!";

        private readonly TaskFactory _factory;

        /// <summary>
        ///     Dictionary(EventType, Dictionary(Sender, EventHandlerTaskFactories))
        /// </summary>
        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<object, ConcurrentBag<object>>> _hub;

        public AsyncEventHub()
        {
            _factory = Task.Factory;

            _hub = new ConcurrentDictionary<Type, ConcurrentDictionary<object, ConcurrentBag<object>>>();
        }

        /// <summary>
        ///     Publishes an event.
        /// </summary>
        /// <typeparam name="TEvent">Event type.</typeparam>
        /// <param name="sender">The source of the event.</param>
        /// <param name="eventDataTask">An event data.</param>
        /// <returns>Subscriber's asynchronous replies.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        ///     <paramref name="sender" /> is a null reference (Nothing in Visual Basic).-or-
        ///     <paramref name="eventDataTask" />
        ///     is a null reference (Nothing in Visual Basic).
        /// </exception>
        public Task<Task[]> Publish<TEvent>(object sender, Task<TEvent> eventDataTask)
        {
            var taskCompletionSource = new TaskCompletionSource<Task[]>();

            if (sender == null)
            {
                taskCompletionSource.SetException(new ArgumentNullException("sender"));
                return taskCompletionSource.Task;
            }

            if (eventDataTask == null)
            {
                taskCompletionSource.SetException(new ArgumentNullException("eventDataTask"));
                return taskCompletionSource.Task;
            }
            Type eventType = typeof (TEvent);

            ConcurrentDictionary<object, ConcurrentBag<object>> subscribers;

            if (_hub.TryGetValue(eventType, out subscribers) && subscribers.Count > 0)
            {
                // Executes event handlers for all receivers except sender
                _factory.ContinueWhenAll(
                    new List<Task>(
                        new List<object>(subscribers.Keys)
                            .Where(p => p != sender)
                            .Select(p =>
                                {
                                    ConcurrentBag<object> eventHandlerTaskFactories;

                                    // Try to get event handlers
                                    if (!subscribers.TryGetValue(p, out eventHandlerTaskFactories) || eventHandlerTaskFactories == null)
                                    {
                                        return null;
                                    }

                                    return eventHandlerTaskFactories;
                                })
                            .SelectMany(
                                p =>
                                    {
                                        if (p == null)
                                        {
                                            var innerTaskCompletionSource = new TaskCompletionSource<Task>();
                                            innerTaskCompletionSource.SetException(new Exception(FailedToGetEventHandlerTaskFactoriesExceptionMessage));
                                            return new ConcurrentBag<Task>(new[] {innerTaskCompletionSource.Task});
                                        }

                                        return new ConcurrentBag<Task>(p.Select(q => ((Func<Task<TEvent>, Task>) q)(eventDataTask)));
                                    }))
                        .ToArray(),
                    taskCompletionSource.SetResult);
            }
            else
            {
                // No event subscribers was found
                taskCompletionSource.SetResult(new Task[] {});
            }

            return taskCompletionSource.Task;
        }

        /// <summary>
        ///     Subscribes on an event.
        /// </summary>
        /// <typeparam name="TEvent">Event type.</typeparam>
        /// <param name="sender">Receiver of the event.</param>
        /// <param name="eventHandlerTaskFactory">Event handler.</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///     <paramref name="sender" /> is a null reference (Nothing in Visual Basic).-or-
        ///     <paramref name="eventHandlerTaskFactory" />
        ///     is a null reference (Nothing in Visual Basic).
        /// </exception>
        public void Subscribe<TEvent>(object sender, Func<Task<TEvent>, Task> eventHandlerTaskFactory)
        {
            if (sender == null)
            {
                throw new ArgumentNullException("sender");
            }

            if (eventHandlerTaskFactory == null)
            {
                throw new ArgumentNullException("eventHandlerTaskFactory");
            }

            Type eventType = typeof (TEvent);

            // Gets an subscribers list
            ConcurrentDictionary<object, ConcurrentBag<object>> subscribers = _hub.GetOrAdd(eventType, type => new ConcurrentDictionary<object, ConcurrentBag<object>>());

            // Gets event handlers list
            ConcurrentBag<object> eventHandlerTaskFactories = subscribers.GetOrAdd(sender, o => new ConcurrentBag<object>());

            // Adds event handler
            eventHandlerTaskFactories.Add(eventHandlerTaskFactory);
        }

        /// <summary>
        ///     Unsubscribes from an event.
        /// </summary>
        /// <typeparam name="TEvent">Event type.</typeparam>
        /// <param name="sender">Receiver of the event.</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///     <paramref name="sender" /> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public void Unsubscribe<TEvent>(object sender)
        {
            if (sender == null)
            {
                throw new ArgumentNullException("sender");
            }

            Type eventType = typeof (TEvent);

            ConcurrentDictionary<object, ConcurrentBag<object>> subscribers;

            // Try to get event subscribers
            if (_hub.TryGetValue(eventType, out subscribers) && subscribers != null)
            {
                ConcurrentBag<object> eventHandlerTaskFactories;

                // Try to remove receiver subscriptions
                if (!subscribers.TryRemove(sender, out eventHandlerTaskFactories))
                {
                    throw new Exception(FailedToRemoveEventHandlerTaskFactories);
                }
            }
            else
            {
                throw new Exception(FailedToGetSubscribersExceptionMessage);
            }
        }
    }
}