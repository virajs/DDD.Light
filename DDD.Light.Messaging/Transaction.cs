﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;

namespace DDD.Light.Messaging
{
    [Serializable]
    public class Transaction<T>
    {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public Transaction()
        {
            
        }

        public Transaction(T @event, IEnumerable<Action<T>> eventHandlers)
        {
            Event = @event;
            Id = Guid.NewGuid();
            ProcessedActions = new List<Action<T>>();
            ErroredOutActions = new Dictionary<Action<T>, Exception>();
            NotProcessedActions = new Queue<Action<T>>(eventHandlers);
        }

        public Guid Id { get; private set; }
        public T Event { get; set; }
        public List<Action<T>> ProcessedActions { get; private set; }
        public IDictionary<Action<T>, Exception> ErroredOutActions { get; private set; }
        public Queue<Action<T>> NotProcessedActions { get; private set; }

        public void Commit()
        {
            while (NotProcessedActions.Count > 0)
            {
                var handler = NotProcessedActions.Dequeue();
                try
                {
                    handler.Invoke(Event);
                    ProcessedActions.Add(handler);
                }
                catch (Exception ex)
                {
                    ErroredOutActions.Add(handler, ex);
                }
            }
            LogCommit();
        }

        private void LogCommit()
        {
            var success = NotProcessedActions.Count == 0 && ErroredOutActions.Count == 0;
            if (success)
                LogSuccessfulCommit();
            else
                LogFailedCommit();
        }

        //todo: adjust text (now command, not only event)
        private void LogFailedCommit()
        {
            var errors = (
                from entry  in ErroredOutActions 
                    let eventHandlerType = entry.Key.GetType().ToString() 
                    let errorMessage = entry.Value.Message 
                    let stackTrace = entry.Value.StackTrace 
                    let innerExceptionMessage = entry.Value.InnerException != null ? entry.Value.InnerException.Message : string.Empty 
                    let innerExceptionStackTrace = entry.Value.InnerException != null ? entry.Value.InnerException.StackTrace : string.Empty 
                select string.Format("EVENT HANDLER: {0} ::: ERROR MESSAGE: {1} ::: STACK TRACE: {2} ::: INNER EXCEPTION MESSAGE: {3} ::: INNER EXCEPTION STACK TRACE: {4}", 
                    eventHandlerType, errorMessage, stackTrace, innerExceptionMessage, innerExceptionStackTrace)
                ).ToList();

            var notProcessedEventHandlerTypes = (
                    from h in NotProcessedActions.ToList()
                    select h.GetType()
                ).ToList();
            
            var processedEventHandlerTypes = (
                    from h in ProcessedActions.ToList()
                    select h.GetType()
                ).ToList();

            var message = string.Format(
                "TRANSACTION ERROR [ID: {0}] " +
                "[EVENT TYPE: {1}] " +
                "[EVENT DATA: {2}] " +
                "[ERRORS: {3}] " +
                "[NOT PROCESSED EVENT HANDLERS: {4}] " +
                "[PROCESSED EVENT HANDLERS: {5}]",
                Id, Event.GetType(), Event, string.Join(" ^^^ ", errors), string.Join(", ", notProcessedEventHandlerTypes), string.Join(", ", processedEventHandlerTypes));

            _log.Error(message);
        }

        //todo: adjust text (now command, not only event)
        private void LogSuccessfulCommit()
        {
            var processedEventHandlerTypes = (
                    from h in ProcessedActions.ToList()
                    select h.GetType()
                ).ToList();

            var message = string.Format(
                "TRANSACTION SUCCESS [ID: {0}] " +
                "[EVENT TYPE: {1}] " +
                "[EVENT DATA: {2}] " +
                "[PROCESSED EVENT HANDLERS: {3}]",
                Id, Event.GetType(), Event, string.Join(", ", processedEventHandlerTypes));

            _log.Error(message);
        }
    }
}