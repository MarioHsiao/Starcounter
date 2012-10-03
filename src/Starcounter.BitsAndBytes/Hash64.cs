public class Hash64 {
    public static long ComputeHashCode(string url) {
        const ulong p = 1099511628211;

        ulong hash = 14695981039346656037;

        for (int i = 0; i < url.Length; ++i) {
            hash = (hash ^ url[i]) * p;
        }

        // Wang64 bit mixer
        hash = (~hash) + (hash << 21);
        hash = hash ^ (hash >> 24);
        hash = (hash + (hash << 3)) + (hash << 8);
        hash = hash ^ (hash >> 14);
        hash = (hash + (hash << 2)) + (hash << 4);
        hash = hash ^ (hash >> 28);
        hash = hash + (hash << 31);

//        if (hash == (ulong)UNKNOWN_RECORD_HASH) {
//            ++hash;
//        }
        return (long)hash;
    }
}