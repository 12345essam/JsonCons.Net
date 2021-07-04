﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
        
namespace JsonCons.JsonPathLib
{
    struct CacheEntry
    {
        internal CacheEntry(PathNode pathStem, JsonElement value)
        {
            PathStem = pathStem;
            Value = value;
        }

        internal PathNode PathStem {get;}
        internal JsonElement Value {get;}
    }

    class CacheEntryAccumulator : INodeAccumulator
    {
        internal List<CacheEntry> CacheEntries {get;} = new List<CacheEntry>();

        public void Accumulate(PathNode pathStem, JsonElement value)
        {
            CacheEntries.Add(new CacheEntry(pathStem, value));
        }
    };

    class DynamicResources 
    {
        Dictionary<Int32,IList<CacheEntry>> _cache = new Dictionary<Int32,IList<CacheEntry>>();

        internal bool IsCached(Int32 id)
        {
            return _cache.ContainsKey(id);
        }

        internal void AddToCache(Int32 id, IList<CacheEntry> items) 
        {
            _cache.Add(id, items);
        }

        internal void RetrieveFromCache(Int32 id, INodeAccumulator accumulator) 
        {
            IList<CacheEntry> items;
            if (_cache.TryGetValue(id, out items))
            {
                foreach (var item in items)
                {
                    accumulator.Accumulate(item.PathStem, item.Value);
                }
            }
        }
    };

    public enum ResultOptions {Path=1, NoDups=Path|2, Sort=Path|4};

    public static class JsonPath
    {
        public static bool TrySelect(JsonElement root, NormalizedPath path, out JsonElement element)
        {
            element = root;
            foreach (var pathNode in path)
            {
                if (pathNode.NodeKind == PathNodeKind.Index)
                {
                    if (element.ValueKind != JsonValueKind.Array || pathNode.GetIndex() >= element.GetArrayLength())
                    {
                        return false; 
                    }
                    element = element[pathNode.GetIndex()];
                }
                else if (pathNode.NodeKind == PathNodeKind.Name)
                {
                    if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(pathNode.GetName(), out element))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool TrySelect(IJsonValue root, NormalizedPath path, out IJsonValue value)
        {
            value = root;
            foreach (var pathNode in path)
            {
                if (pathNode.NodeKind == PathNodeKind.Index)
                {
                    if (value.ValueKind != JsonValueKind.Array || pathNode.GetIndex() >= value.GetArrayLength())
                    {
                        return false; 
                    }
                    value = value[pathNode.GetIndex()];
                }
                else if (pathNode.NodeKind == PathNodeKind.Name)
                {
                    if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(pathNode.GetName(), out value))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static JsonPathExpression Compile(string expr)
        {

            var compiler = new JsonPathCompiler(expr);
            return compiler.Compile();
        }

    }

    public class JsonPathExpression : IDisposable
    {
        private bool _disposed = false;
        StaticResources _resources;
        readonly PathExpression _expr;
        ResultOptions _requiredOptions;

        internal JsonPathExpression(StaticResources resources, ISelector selector, bool pathsRequired)
        {
            _resources = resources;
            _expr = new PathExpression(selector);
            if (pathsRequired)
            {
                _requiredOptions = ResultOptions.Path;
            }
        }

        public IReadOnlyList<JsonElement> Select(JsonElement root, ResultOptions options = 0)
        {
            return _expr.Select(root, options | _requiredOptions);
        }

        public IReadOnlyList<NormalizedPath> SelectPaths(JsonElement root, ResultOptions options = 0)
        {
            return _expr.SelectPaths(root, options | _requiredOptions);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    _resources.Dispose();
                }
                _disposed = true;
            }
        }

        ~JsonPathExpression()
        {
            Dispose(false);
        }
    }

} // namespace JsonCons.JsonPathLib
