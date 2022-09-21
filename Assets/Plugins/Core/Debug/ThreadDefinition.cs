using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Async
{
    public static class ThreadDefinition
    {
        public const string managerThread = "Managers thread";
        public const string aiThread = "AIs thread";
        public const string contextThread = "Context thread";
        public const string worldTread = "World thread";
        public const string systemsTread = "Systems thread";
        public const string servicesTread = "Services";
    }
}
