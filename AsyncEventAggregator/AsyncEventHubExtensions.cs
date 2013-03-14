using System;
using System.Threading.Tasks;

namespace AsyncEventAggregator
{
    /// <summary>
    ///     Asynchronous extensions for an <see cref="AsyncEventHub" />.
    /// </summary>
    public static class AsyncEventHubExtensions
    {
        private static readonly AsyncEventHub AsyncEventHub = new AsyncEventHub();

        /// <summary>
        ///     Publishes an asynchronous event.
        /// </summary>
        /// <typeparam name="TEvent">Event type.</typeparam>
        /// <param name="sender">The source of the event.</param>
        /// <param name="eventData">An event data.</param>
        /// <returns>Subscriber's asynchronous replies.</returns>
        public static Task<Task[]> Publish<TEvent>(this object sender, Task<TEvent> eventData)
        {
            return AsyncEventHub.Publish(sender, eventData);
        }

        /// <summary>
        ///     Publishes an event.
        /// </summary>
        /// <typeparam name="TEvent">Event type.</typeparam>
        /// <param name="sender">The source of the event.</param>
        /// <param name="eventData">An event data.</param>
        /// <returns>Subscriber's asynchronous replies.</returns>
        public static Task<Task[]> Publish<TEvent>(this object sender, TEvent eventData)
        {
            var taskCompletionSource = new TaskCompletionSource<TEvent>();

            taskCompletionSource.SetResult(eventData);

            return AsyncEventHub.Publish(sender, taskCompletionSource.Task);
        }

        /// <summary>
        ///     Subscribes on an event.
        /// </summary>
        /// <typeparam name="TEvent">Event type.</typeparam>
        /// <param name="sender">Receiver of the event.</param>
        /// <param name="eventHandlerTaskFactory">Event handler.</param>
        /// <returns>Operation result.</returns>
        public static void Subscribe<TEvent>(this object sender, Func<Task<TEvent>, Task> eventHandlerTaskFactory)
        {
            AsyncEventHub.Subscribe(sender, eventHandlerTaskFactory);
        }

        /// <summary>
        ///     Unsubscribes from an event.
        /// </summary>
        /// <typeparam name="TEvent">Event type.</typeparam>
        /// <param name="sender">Receiver of the event.</param>
        /// <returns>Operation result.</returns>
        public static void Unsubscribe<TEvent>(this object sender)
        {
            AsyncEventHub.Unsubscribe<TEvent>(sender);
        }
    }
}