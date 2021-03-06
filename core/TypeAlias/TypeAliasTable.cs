﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace TypeAlias
{
    public class TypeAliasTable
    {
        private Dictionary<int, Type> _alias2TypeMap;
        private Dictionary<Type, int> _type2AliasMap;

        public TypeAliasTable()
        {
            BuildTable(TypeUtility.AssemblyProvider.GetAssemblies(), null, null);
        }

        public TypeAliasTable(IEnumerable<Assembly> assemblies,
                              Func<Type, bool> filter = null,
                              Func<Type, int> resolveAutoAlias = null)
        {
            BuildTable(assemblies, filter, resolveAutoAlias);
        }

        private void BuildTable(IEnumerable<Assembly> assemblies,
                                Func<Type, bool> filter,
                                Func<Type, int> resolveAutoAlias)
        {
            _alias2TypeMap = new Dictionary<int, Type>();
            _type2AliasMap = new Dictionary<Type, int>();

            foreach (var assembly in assemblies)
            {
                foreach (var item in TypeUtility.GetTypesContainingAttribute<TypeAliasAttribute>(assembly))
                {
                    if (filter != null && filter(item.Type) == false)
                        continue;

                    var alias = item.Attribute.Alias;
                    if (alias == 0)
                    {
                        alias = resolveAutoAlias != null
                                    ? resolveAutoAlias(item.Type)
                                    : CalculateAlias(item.Type.FullName);
                    }

                    AddTypeAlias(item.Type, alias);
                }
            }
        }

        public void AddTypeAlias(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            var alias = CalculateAlias(type.FullName);
            AddTypeAlias(type, alias);
        }

        public void AddTypeAlias(Type type, int alias)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (alias <= 0)
                throw new ArgumentOutOfRangeException("alias");

            Type conflictType;
            if (_alias2TypeMap.TryGetValue(alias, out conflictType))
            {
                throw new InvalidOperationException(
                    string.Format("Alias conflict. {0} and {1} have same alias {2}.",
                                  type.FullName, conflictType.FullName, alias));
            }

            _type2AliasMap.Add(type, alias);
            _alias2TypeMap.Add(alias, type);
        }

        public Type GetType(int alias)
        {
            Type type;
            return _alias2TypeMap.TryGetValue(alias, out type) ? type : null;
        }

        public int GetAlias(Type type)
        {
            int alias;
            return _type2AliasMap.TryGetValue(type, out alias) ? alias : 0;
        }

        public static int CalculateAlias(string name)
        {
            // make hash [0x100, 0x7FFFFFFF]
            var ihash = (int)(CalculateDjb2Hash(name) & 0x7FFFFFFF);
            var phash = ihash == 0 ? 1 : ihash;
            return (phash < 256)
                       ? phash = phash * 131
                       : phash;
        }

        private static uint CalculateDjb2Hash(string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;

            uint hash = 5381;

            var len = str.Length;
            for (var i = 0; i < len; i++)
                hash = ((hash << 5) + hash) + str[i];

            return hash;
        }
    }
}
