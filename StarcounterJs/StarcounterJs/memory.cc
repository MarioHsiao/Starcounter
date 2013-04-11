
#include <sccoredbg.h>
#include <stdint.h>


extern void *_vm_reserve(size_t size);
extern void *_vm_commit(void *pmem, size_t size, int32_t except);
extern void _vm_decommit(void *pmem, size_t size);
extern void _vm_release(void *pmem, size_t size);


#if 1
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif // WIN32_LEAN_AND_MEAN
#include <Windows.h>

void *_vm_reserve(size_t size)
{
	return VirtualAlloc(NULL, size, MEM_RESERVE, PAGE_READWRITE);
}

void *_vm_commit(void *pmem, size_t size, int32_t except)
{
	void *p;
	p = VirtualAlloc(pmem, size, MEM_COMMIT, PAGE_READWRITE);
	if (p) return p;
	if (except) return sccoredbg_critical_out_of_memory();
	return p;
}

void _vm_decommit(void *pmem, size_t size)
{
	VirtualFree(pmem, size, MEM_DECOMMIT);
}

void _vm_release(void *pmem, size_t size)
{
	VirtualFree(pmem, 0, MEM_RELEASE);
}
#endif


#if 0
#include <memory.h>
#include <sys/mman.h>


void `*_vm_reserve(size_t size)
{
	return mmap(0, size, PROT_READ | PROT_WRITE, MAP_PRIVATE | MAP_ANONYMOUS, 0, 0);
}

void *_vm_commit(void *pmem, size_t size, int32_t except)
{
	if (pmem == NULL) return _vm_reserve(size);
	return pmem;
}

void _vm_decommit(void *pmem, size_t size)
{
	mmap(pmem, size, PROT_READ | PROT_WRITE, MAP_FIXED | MAP_PRIVATE, 0, 0);
}

void _vm_release(void *pmem, size_t size)
{
	munmap(pmem, size);
}
#endif
