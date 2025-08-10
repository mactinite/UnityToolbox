namespace toolbox.DamageSystem
{
    public class DamageBase
    {
        protected float amount;
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