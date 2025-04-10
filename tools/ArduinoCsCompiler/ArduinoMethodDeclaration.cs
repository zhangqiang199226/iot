﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using Iot.Device.Arduino;

#pragma warning disable CS1591
namespace ArduinoCsCompiler
{
    internal sealed class ArduinoMethodDeclaration
    {
        /// <summary>
        /// This is used only for special cases, where the il code is directly generated by our compiler and therefore also its properties cannot be derived from the header
        /// </summary>
        public ArduinoMethodDeclaration(int token, EquatableMethod methodBase, AnalysisStack stack, MethodFlags flags, int maxLocals, int maxStack, IlCode code)
        {
            Index = -1;
            MethodBase = methodBase;
            Stack = stack.Clone();
            Flags = flags;
            Token = token;
            MaxLocals = maxLocals;
            MaxStack = maxStack;
            HasBody = code.IlBytes != null;
            NativeMethod = 0;
            Code = code;
            ArgumentCount = methodBase.GetParameters().Length;
            if (methodBase.CallingConvention.HasFlag(CallingConventions.HasThis))
            {
                ArgumentCount += 1;
            }

            if (Code.ExceptionClauses != null && Code.ExceptionClauses.Any())
            {
                Flags |= MethodFlags.ExceptionClausesPresent;
            }

            Name = $"{MethodBase.MethodSignature()} (Token 0x{Token:X})";
        }

        public ArduinoMethodDeclaration(int token, EquatableMethod methodBase, AnalysisStack stack, IlCode code, MethodFlags extraFlags)
        {
            Index = -1;
            MethodBase = methodBase;
            Stack = stack.Clone();
            Code = code;
            Flags = extraFlags;
            Token = token;

            var attribs = methodBase.GetCustomAttributes(typeof(ArduinoImplementationAttribute)).Cast<ArduinoImplementationAttribute>().ToList();

            if (methodBase.IsAbstract)
            {
                MaxLocals = 0;
                MaxStack = 0;
                HasBody = false;
                NativeMethod = 0;
            }
            else if (attribs.Any(x => x.MethodNumber != 0))
            {
                MaxLocals = 0;
                MaxStack = 0;
                HasBody = false;
                NativeMethod = attribs.First().MethodNumber;
                Flags |= MethodFlags.SpecialMethod;
            }
            else
            {
                var body = methodBase.GetMethodBody();
                if (body == null)
                {
                    throw new InvalidOperationException("Use this ctor only for methods that have a body");
                }

                MaxLocals = body.LocalVariables.Count;
                MaxStack = body.MaxStackSize;
                HasBody = true;
                NativeMethod = 0;
            }

            ArgumentCount = methodBase.GetParameters().Length;
            if (methodBase.CallingConvention.HasFlag(CallingConventions.HasThis))
            {
                ArgumentCount += 1;
            }

            if (methodBase.IsStatic)
            {
                Flags |= MethodFlags.Static;
            }

            if (methodBase.IsVirtual)
            {
                Flags |= MethodFlags.Virtual;
            }

            if (methodBase.Method is MethodInfo methodInfo)
            {
                if (methodInfo.ReturnParameter == null || methodInfo.ReturnParameter.ParameterType == typeof(void))
                {
                    Flags |= MethodFlags.Void;
                }
            }
            else if (methodBase.Method is ConstructorInfo ctorInfo && ctorInfo.IsStatic)
            {
                Flags |= MethodFlags.Static | MethodFlags.Void;
            }

            if (methodBase.IsConstructor && !methodBase.IsStatic)
            {
                Flags |= MethodFlags.Ctor;
            }

            if (methodBase.IsAbstract)
            {
                Flags |= MethodFlags.Abstract;
            }

            if (Code.ExceptionClauses != null && Code.ExceptionClauses.Any())
            {
                Flags |= MethodFlags.ExceptionClausesPresent;
            }

            Name = $"{MethodBase.MethodSignature()} (Token 0x{Token:X})";
        }

        public ArduinoMethodDeclaration(int token, EquatableMethod methodBase, AnalysisStack stack, MethodFlags flags, int nativeMethod)
        {
            Index = -1;
            Token = token;
            MethodBase = methodBase;
            Flags = flags;
            NativeMethod = nativeMethod;
            MaxLocals = MaxStack = 0;
            HasBody = false;
            Stack = stack;
            Code = new IlCode(methodBase, null);
            ArgumentCount = methodBase.GetParameters().Length;
            if (methodBase.CallingConvention.HasFlag(CallingConventions.HasThis))
            {
                ArgumentCount += 1;
            }

            MethodInfo? mi = methodBase.Method as MethodInfo;

            if (mi != null && mi.ReturnParameter.ParameterType == typeof(void))
            {
                Flags |= MethodFlags.Void;
            }

            if (methodBase.IsConstructor && !methodBase.IsStatic)
            {
                Flags |= MethodFlags.Ctor;
            }

            if (methodBase.IsStatic)
            {
                Flags |= MethodFlags.Static;
            }

            if (methodBase.IsVirtual)
            {
                Flags |= MethodFlags.Virtual;
            }

            Name = $"{MethodBase.MethodSignature()} (Special Method, Token 0x{Token:X})";
        }

        public bool HasBody
        {
            get;
        }

        public int Index
        {
            get;
            internal set;
        }

        public string Name
        {
            get;
        }

        public int Token { get; }
        public EquatableMethod MethodBase { get; }

        /// <summary>
        /// To simplify backtracking: The call stack when the method was first parsed.
        /// </summary>
        public AnalysisStack Stack { get; }

        public IlCode Code { get; }

        public MethodInfo MethodInfo
        {
            get
            {
                if (MethodBase.Method is MethodInfo info)
                {
                    return info;
                }

                // Can't directly call fields. Internally, we do call ctors (especially cctors) directly, because
                // that is required for the init sequence
                throw new InvalidOperationException("Not callable Method");
            }
        }

        public MethodFlags Flags
        {
            get;
        }

        public int NativeMethod { get; }

        public int MaxLocals
        {
            get;
        }

        public int MaxStack
        {
            get;
        }

        public int ArgumentCount
        {
            get;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
