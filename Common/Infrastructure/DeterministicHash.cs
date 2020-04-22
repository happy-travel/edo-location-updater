namespace Common.Infrastructure
{
    public static class DeterministicHash
    {
        /// <summary>
        /// https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core/
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int Calculate(string value)
        {
            //disables overflow-checking for the integer arithmetic done inside the function
            unchecked
            {
                var hash1 = (5381 << 16) + 5381;
                var hash2 = hash1;

                for (var i = 0; i < value.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ value[i];
                    if (i == value.Length - 1)
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ value[i + 1];
                }

                return hash1 + hash2 * 1566083941;
            }
        }
    }
}