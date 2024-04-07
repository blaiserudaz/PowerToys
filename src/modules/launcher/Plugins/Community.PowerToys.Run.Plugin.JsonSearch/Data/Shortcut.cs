// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Community.PowerToys.Run.Plugin.JsonSearch.Data
{
    public class Shortcut
    {
#pragma warning disable SA1300

        public string keyword { get; set; } = string.Empty;

        public string title { get; set; } = string.Empty;

        public string icon { get; set; } = string.Empty;

        public string url { get; set; } = string.Empty;

        public string path { get; set; } = string.Empty;

        public string param { get; set; } = string.Empty;

        public List<string>? tags { get; set; }
    }
}
