using System;
using System.Collections.Generic;
using System.Linq;

namespace SpacetimeDB
{
    public interface IDatabaseTable
    {
        void InternalOnValueInserted();
        void InternalOnValueDeleted();
        void OnInsertEvent(ClientApi.Event dbEvent);
        void OnBeforeDeleteEvent(ClientApi.Event dbEvent);
        void OnDeleteEvent(ClientApi.Event dbEvent);
    }

    public abstract class DatabaseTable<T, ReducerEvent> : IDatabaseTable where T : IDatabaseTable where ReducerEvent : ReducerEventBase
    {
        public virtual void InternalOnValueInserted() { }

        public virtual void InternalOnValueDeleted() { }

        public static IEnumerable<T> Iter()
        {
            return SpacetimeDBClient.clientDB.GetObjects<T>();
        }

        public static IEnumerable<T> Query(Func<T, bool> filter)
        {
            return Iter().Where(filter);
        }

        public static int Count()
        {
            return SpacetimeDBClient.clientDB.Count<T>();
        }

        public delegate void InsertEventHandler(T insertedValue, ReducerEvent? dbEvent);
        public delegate void DeleteEventHandler(T deletedValue, ReducerEvent? dbEvent);
        public delegate void RowUpdateEventHandler(SpacetimeDBClient.TableOp op, T? oldValue, T? newValue, ReducerEvent? dbEvent);
        public static event InsertEventHandler? OnInsert;
        public static event DeleteEventHandler? OnBeforeDelete;
        public static event DeleteEventHandler? OnDelete;

        // We need this because C# doesn't allow to refer to self type.
        protected abstract T GetThis();

        public void OnInsertEvent(ClientApi.Event dbEvent)
        {
            OnInsert?.Invoke(GetThis(), (ReducerEvent?)dbEvent?.FunctionCall.CallInfo);
        }

        public void OnBeforeDeleteEvent(ClientApi.Event dbEvent)
        {
            OnBeforeDelete?.Invoke(GetThis(), (ReducerEvent?)dbEvent?.FunctionCall.CallInfo);
        }

        public void OnDeleteEvent(ClientApi.Event dbEvent)
        {
            OnDelete?.Invoke(GetThis(), (ReducerEvent?)dbEvent?.FunctionCall.CallInfo);
        }
    }

    public interface IDatabaseTableWithPrimaryKey : IDatabaseTable
    {
        void OnUpdateEvent(IDatabaseTableWithPrimaryKey newValue, ClientApi.Event dbEvent);
        object GetPrimaryKeyValue();
    }

    public abstract class DatabaseTableWithPrimaryKey<T, ReducerEvent> : DatabaseTable<T, ReducerEvent>, IDatabaseTableWithPrimaryKey
        where T : IDatabaseTableWithPrimaryKey
        where ReducerEvent : ReducerEventBase
    {
        public abstract object GetPrimaryKeyValue();

        public delegate void UpdateEventHandler(T oldValue, T newValue, ReducerEvent? dbEvent);
        public static event UpdateEventHandler? OnUpdate;

        public void OnUpdateEvent(IDatabaseTableWithPrimaryKey newValue, ClientApi.Event dbEvent)
        {
            OnUpdate?.Invoke(GetThis(), (T)newValue, (ReducerEvent?)dbEvent?.FunctionCall.CallInfo);
        }
    }
}
