﻿using DurableTask.Core;
using System;
using System.Collections.Generic;

namespace maskx.OrchestrationService
{
    public class DynamicNameVersionObjectManager<T> : INameVersionObjectManager<T>
    {
        private readonly IDictionary<string, ObjectCreator<T>> creators;
        private readonly object thisLock = new object();

        public DynamicNameVersionObjectManager()
        {
            this.creators = new Dictionary<string, ObjectCreator<T>>();
        }

        public void Remove(ObjectCreator<T> creator)
        {
            lock (this.thisLock)
            {
                string key = GetKey(creator.Name, creator.Version);
                this.creators.Remove(key);
            }
        }

        public void TryAdd(ObjectCreator<T> creator)
        {
            lock (this.thisLock)
            {
                string key = GetKey(creator.Name, creator.Version);

                if (!this.creators.ContainsKey(key))
                {
                    this.creators.Add(key, creator);
                }
            }
        }

        public void Add(ObjectCreator<T> creator)
        {
            lock (this.thisLock)
            {
                string key = GetKey(creator.Name, creator.Version);

                if (this.creators.ContainsKey(key))
                {
                    throw new InvalidOperationException("Duplicate entry detected: " + creator.Name + " " +
                                                        creator.Version);
                }

                this.creators.Add(key, creator);
            }
        }

        public T GetObject(string name, string version)
        {
            string key = GetKey(name, version);

            lock (this.thisLock)
            {
                if (this.creators.TryGetValue(key, out ObjectCreator<T> creator))
                    return creator.Create();
                throw new KeyNotFoundException($"cannot find {key} in DynamicNameVersionObjectManager");
            }
        }

        public ObjectCreator<T> GetCreator(string key)
        {
            if (this.creators.ContainsKey(key))
                return this.creators[key];
            throw new KeyNotFoundException($"cannot find {key} in DynamicNameVersionObjectManager");
        }

        private string GetKey(string name, string version)
        {
            return name + "_" + version;
        }
    }
}