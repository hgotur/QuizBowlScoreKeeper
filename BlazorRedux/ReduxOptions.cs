﻿using System;
using System.Text.Json;

namespace BlazorRedux
{
    public class ReduxOptions<TState>
    {
        public ReduxOptions()
        {
            // Defaults
            StateSerializer = state => JsonSerializer.Serialize(state);
            StateDeserializer = str => JsonSerializer.Deserialize<TState>(str);
        }

        public Reducer<TState, NewLocationAction> LocationReducer { get; set; }
        public Func<TState, string> GetLocation { get; set; }
        public Func<TState, string> StateSerializer { get; set; }
        public Func<string, TState> StateDeserializer { get; set; }
    }
}