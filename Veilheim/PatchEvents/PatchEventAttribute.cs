﻿// Veilheim
// a Valheim mod
// 
// File:    PatchEventAttribute.cs
// Project: Veilheim

using System;

namespace Veilheim.PatchEvents
{
    [AttributeUsage(AttributeTargets.Method)]
    public class PatchEventAttribute : Attribute
    {
        public PatchEventAttribute(Type classToPatch, string methodName, PatchEventType eventType, int priority = 500)
        {
            EventType = eventType;
            ClassToPatch = classToPatch;
            MethodName = methodName;
            Priority = priority;
        }

        public PatchEventType EventType { get; set; }
        public Type ClassToPatch { get; set; }
        public string MethodName { get; set; }
        public int Priority { get; set; } = 500;
    }
}