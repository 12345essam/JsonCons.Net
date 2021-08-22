﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.RegularExpressions;
        
namespace JsonCons.JmesPath
{
    interface IUnaryOperator 
    {
        int PrecedenceLevel {get;}
        bool IsRightAssociative {get;}
        bool TryEvaluate(IValue elem, out IValue result);
    };

    abstract class UnaryOperator : IUnaryOperator
    {
        internal UnaryOperator(int precedenceLevel,
                                bool isRightAssociative = false)
        {
            PrecedenceLevel = precedenceLevel;
            IsRightAssociative = isRightAssociative;
        }

        public int PrecedenceLevel {get;} 

        public bool IsRightAssociative {get;} 

        public abstract bool TryEvaluate(IValue elem, out IValue result);
    };

    sealed class NotOperator : UnaryOperator
    {
        internal static NotOperator Instance { get; } = new NotOperator();

        internal NotOperator()
            : base(1, true)
        {}

        public override bool TryEvaluate(IValue val, out IValue result)
        {
            result = Expression.IsFalse(val) ? JsonConstants.True : JsonConstants.False;
            return true;
        }

        public override string ToString()
        {
            return "Not";
        }
    };

    sealed class RegexOperator : UnaryOperator
    {
        Regex _regex;

        internal RegexOperator(Regex regex)
            : base(7, true)
        {
            _regex = regex;
        }

        public override bool TryEvaluate(IValue val, out IValue result)
        {
            if (!(val.Type == JmesPathType.String))
            {
                result = JsonConstants.Null;
                return false; // type error
            }
            result = _regex.IsMatch(val.GetString()) ? JsonConstants.True : JsonConstants.False;
            return true;
        }

        public override string ToString()
        {
            return "Regex";
        }
    };

} // namespace JsonCons.JmesPath

