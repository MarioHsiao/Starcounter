using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

namespace tpcc
{
  public class TpccValuesGenerator
  {
    private Random rand = new Random();

    //random value generators according to TPCC 4.3.2
    public int random(int low, int high)
    {
      lock(rand)
      {
        return rand.Next(low, high + 1);
      }
    }

    public decimal random(decimal low, decimal high, int prec)
    {
      int scale = 1;
      while (prec > 0)
      {
        scale *= 10;
        --prec;
      }

      return new decimal(random(decimal.ToInt32(low * scale), decimal.ToInt32(high * scale))) / scale;
    }

    private T rand_elem<T>(T[] set)
    {
      return set[random(0, set.Length - 1)];
    }

    private T[] rand_seq<T>(T[] set, int len)
    {
      T[] res = new T[len];
      for (int i = 0; i < len; ++i)
        res[i] = rand_elem(set);

      return res;
    }

    public string a_string(int low, int high)
    {
      int len = random(low, high);
      return new string(rand_seq("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray(), len));
    }

    public string n_string(int low, int high)
    {
      int len = random(low, high);
      return new string(rand_seq("0123456789".ToCharArray(), len));
    }

    public bool true_with_probability(int percents)
    {
      return random(1, 100) <= percents;
    }

    //accoridng to 4.3.3.1
    public string generate_I_DATA_or_S_DATA()
    {
      string res = a_string(26, 50);
      bool is_original = true_with_probability(10);
      if (is_original)
      {
        int position = random(0, res.Length - "ORIGINAL".Length);
        res = new StringBuilder(res).Remove(position, "ORIGINAL".Length).Insert(position, "ORIGINAL").ToString();
      }
      return res;
    }

    public string generate_zip()
    {
      return n_string(4, 4) + "11111";
    }

    //according to TPCC 2.1.6
    private Dictionary<int, int> C_from_A_dictionary = new Dictionary<int, int>();
    private int get_C_from_A(int A)
    {
      lock (C_from_A_dictionary)
      {
        int C;
        if (!C_from_A_dictionary.TryGetValue(A, out C))
        {
          C = random(0, A);
          C_from_A_dictionary.Add(A, C);
        }

        return C;
      }
    }

    public int NURand(int A, int x, int y)
    {
      return (((random(0, A) | random(x, y)) + get_C_from_A(A)) % (y - x + 1)) + x;
    }

    //according to 4.3.2.3
    private static string[] last_name_parts = { "BAR", "OUGHT", "ABLE", "PRI", "PRES", "ESE", "ANTI", "CALLY", "ATION", "EING" };
    public string generate_C_LAST(int code)
    {

      return last_name_parts[code / 100] + last_name_parts[code / 10 % 10] + last_name_parts[code % 10];
    }

    //inside-out version of Fisher–Yates shuffle algorithm
    //https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle#The_.22inside-out.22_algorithm
    public IEnumerable<T> shuffle<T>(IEnumerable<T> source, int initial_capacity = 0)
    {
      List<T> res = new List<T>(initial_capacity);

      foreach (var s in source)
      {
        int j = random(0, res.Count);
        if (j == res.Count)
        {
          res.Add(s);
        }
        else
        {
          res.Add(res[j]);
          res[j] = s;
        }
      }

      return res;
    }

    public IEnumerable<int> permutate(int from, int to)
    {
      int n = to - from + 1;
      return shuffle(Enumerable.Range(from, n), n);
    }

    //according to TPCC 2.4.1.5.2
    public int generate_OL_SUPPLY_W_ID(int home_w_id)
    {
#pragma warning disable 162
      if (DataLoader.W_ID_limit == 1)
        return 1;
#pragma warning restore

      if (true_with_probability(99))
        return home_w_id;

      int w_id = random(1, DataLoader.W_ID_limit - 1);
      if (w_id == home_w_id)
        ++w_id;
      return w_id;
    }

    //according to TPCC 2.5.1.2
    public KeyValuePair<int, int> generate_C_W_ID_and_C_D_ID(int home_w_id, int home_d_id)
    {
      if (true_with_probability(85))
      {
        return new KeyValuePair<int, int>(home_w_id, home_d_id);
      }
      else
      {
        int d_id = random(1, 10);
        int w_id;

        if (DataLoader.W_ID_limit == 1)
        {
#pragma warning disable 162
          w_id = 1;
#pragma warning restore
        }
        else
        {
          w_id = random(1, DataLoader.W_ID_limit - 1);
          if (w_id == home_w_id)
            ++w_id;
        }

        return new KeyValuePair<int, int>(w_id, d_id);
      }
    }
  }
}
