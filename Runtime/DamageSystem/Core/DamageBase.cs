using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace toolbox.DamageSystem
{
    [Serializable]
    public class DamageBase
    {
        [SerializeField] protected float amount;
        protected float newHealth;

        public float Amount
        {
            get => amount;
            set => amount = value;
        }

        public float NewHealth
        {
            get => newHealth;
            set => newHealth = value;
        }

        public DamageBase()
        {
            Amount = 1;
        }

        public DamageBase(float amount)
        {
            Amount = amount;
        }
    }
}