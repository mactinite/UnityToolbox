using System;
using mactinite.ToolboxCommons.Singleton;
using UnityEngine;

namespace mactinite.ToolboxCommons.ServiceLocator
{
    [DefaultExecutionOrder(-1000)]
    public class ServiceBundleLoader : PersistentSingletonBehaviour<ServiceBundleLoader>
    {
        public GameObject serviceBundlePrefab;
        public bool isLoaded = false;

        protected override void Awake()
        {
            base.Awake();
            LoadServiceBundle();
        }

        private void LoadServiceBundle()
        {
            if (isLoaded)
                return;

            if (serviceBundlePrefab == null)
            {
                Debug.LogError("ServiceBundleLoader: ServiceBundlePrefab is not set.");
                return;
            }

            Instantiate(serviceBundlePrefab, transform, true);
            isLoaded = true;
        }
    }
}
