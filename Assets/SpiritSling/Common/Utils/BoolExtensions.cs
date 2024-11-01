﻿// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;

namespace SpiritSling
{
    public static class BoolExtensions
    {
        public static void IfTrue(this bool b, Action action)
        {
            if (b) action?.Invoke();
        }

        public static void IfFalse(this bool b, Action action)
        {
            if (!b) action?.Invoke();
        }

        public static void IfElse(this bool b, Action trueAction, Action falseAction)
        {
            if (b) trueAction?.Invoke();
            else falseAction?.Invoke();
        }
    }
}