// Functions from external DLL.
extern "C" int external_dll1_func();
extern "C" int external_dll2_func();

extern "C" int external_dll1_1_func();
extern "C" int external_dll1_2_func();
extern "C" int external_dll1_3_func();
extern "C" int external_dll1_4_func();
extern "C" int external_dll1_5_func();
extern "C" int external_dll1_6_func();
extern "C" int external_dll1_7_func();
extern "C" int external_dll1_8_func();
extern "C" int external_dll1_9_func();
extern "C" int external_dll1_10_func();

extern "C" int external_dll2_1_func();
extern "C" int external_dll2_2_func();
extern "C" int external_dll2_3_func();
extern "C" int external_dll2_4_func();
extern "C" int external_dll2_5_func();
extern "C" int external_dll2_6_func();
extern "C" int external_dll2_7_func();
extern "C" int external_dll2_8_func();
extern "C" int external_dll2_9_func();
extern "C" int external_dll2_10_func();

// Function that is exported from this LLVM DLL.
extern "C" __declspec(dllexport) int gen_function(int p) {
	return external_dll1_func() + external_dll2_func() + 12345 + p +
		external_dll1_1_func() +
		external_dll1_2_func() +
		external_dll1_3_func() +
		external_dll1_4_func() +
		external_dll1_5_func() +
		external_dll1_6_func() +
		external_dll1_7_func() +
		external_dll1_8_func() +
		external_dll1_9_func() +
		external_dll1_10_func() +
		external_dll2_1_func() +
		external_dll2_2_func() +
		external_dll2_3_func() +
		external_dll2_4_func() +
		external_dll2_5_func() +
		external_dll2_6_func() +
		external_dll2_7_func() +
		external_dll2_8_func() +
		external_dll2_9_func() +
		external_dll2_10_func();
}
