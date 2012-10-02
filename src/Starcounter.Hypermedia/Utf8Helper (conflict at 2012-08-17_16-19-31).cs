
namespace Starcounter.Internal.Web {
   public class Utf8Helper {

      public static unsafe uint WriteUIntAsUtf8(byte* pret, uint value) {
         uint i = 0;
         while (value != 0) {
            pret[i++] = (byte)(value % 10 + 0x30);
            value = value / 10;
         }
         return i;
      }

   }
}
