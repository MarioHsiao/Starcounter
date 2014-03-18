
#pragma once

#include <xmemory>
#include <stdint.h>
#if defined(_MSC_VER)
# define WIN32_LEAN_AND_MEAN
# include <windows.h>
# undef WIN32_LEAN_AND_MEAN
#endif // defined(_MSC_VER)

namespace starcounter {
namespace core {

class out_of_memory_exception { };
class not_supported_exception { };


template <class PointedType>
class offset_ptr
{
	typedef offset_ptr<PointedType> self_t;

	void unspecified_bool_type_func() const {}
	typedef void (self_t::*unspecified_bool_type)() const;

	void set_offset(const void *ptr)
	{
		if(!ptr)
			internal.m_offset = 0xFFFFFFFF;
		else
			internal.m_offset = (int64_t)((const char*)ptr - (const char*)(this));
	}

	#if defined(_MSC_VER) && (_MSC_VER >= 1400)
	__declspec(noinline) //this workaround is needed for msvc-8.0 and msvc-9.0
	#endif
	void* get_pointer() const
	{
		return (internal.m_offset != 1) ? (const_cast<char*>(reinterpret_cast<const char*>(this)) + internal.m_offset) : 0;
	}

	void inc_offset(std::ptrdiff_t bytes)
	{
		internal.m_offset += bytes;
	}

	void dec_offset(std::ptrdiff_t bytes)
	{
		internal.m_offset -= bytes;
	}

	union internal_type
	{
		int64_t m_offset;
	} internal;

public:
	typedef PointedType* pointer;
	typedef PointedType& reference;
	typedef PointedType value_type;

public:
	offset_ptr(pointer ptr = 0)
	{
		this->set_offset(ptr);
	}

	template <class T> offset_ptr(T *ptr)
	{
		this->set_offset(ptr);
	}

	offset_ptr(const offset_ptr& ptr)
	{
		this->set_offset(ptr.get());
	}

	template<class T2>
	offset_ptr(const offset_ptr<T2> &ptr) 
	{
		pointer p(ptr.get());
		this->set_offset(p);
	}

	pointer get() const
	{
		return static_cast<pointer>(this->get_pointer());
	}

	std::ptrdiff_t get_offset() const
	{
		return internal.m_offset;
	}

	pointer operator->() const           
	{
		return this->get();
	}

	reference operator* () const           
	{
		pointer p = this->get();
		reference r = *p;
		return r;
	}

	reference operator[] (std::ptrdiff_t idx) const   
	{
		return this->get()[idx];
	}

	operator pointer () const
	{
		return this->get();
	}

	offset_ptr& operator= (pointer from)
	{
		this->set_offset(from);
		return *this;
	}

	offset_ptr& operator= (const offset_ptr & pt)
	{
		pointer p(pt.get());
		this->set_offset(p);
		return *this;
	}

	template <class T2>
	offset_ptr& operator= (const offset_ptr<T2> & pt)
	{
		pointer p(pt.get());
		this->set_offset(p);
		return *this;
	}
 
	offset_ptr operator+ (std::ptrdiff_t offset) const   
	{
		return offset_ptr(this->get() + offset);
	}

	offset_ptr operator- (std::ptrdiff_t offset) const   
	{
		return offset_ptr(this->get() - offset);
	}

	offset_ptr &operator+= (std::ptrdiff_t offset)
	{
		this->inc_offset(offset * sizeof (PointedType));
		return *this;
	}

	offset_ptr &operator-= (std::ptrdiff_t offset)
	{
		this->dec_offset(offset * sizeof (PointedType));
		return *this;
	}

	offset_ptr& operator++ (void) 
	{
		this->inc_offset(sizeof (PointedType));
		return *this;
	}

	offset_ptr operator++ (int)
	{
		offset_ptr temp(*this);
		++*this;
		return temp;
	}

	offset_ptr& operator-- (void) 
	{
		this->dec_offset(sizeof (PointedType));
		return *this;
	}

	offset_ptr operator-- (int)
	{
		offset_ptr temp(*this);
		--*this;
		return temp;
	}

	operator unspecified_bool_type() const  
	{
		return this->get() ? &self_t::unspecified_bool_type_func : 0;
	}

	bool operator ! () const
	{
		return this->get() == 0;
	}

	inline bool operator == (offset_ptr& a)
	{
		return get_offset() == a.get_offset();
	}

	inline bool operator != (offset_ptr& a)
	{
		return !operator==(a);
	}

	inline bool operator == (std::size_t a)
	{
		return this->get() == (void *)a;
	}

	inline bool operator != (std::size_t a)
	{
		return !operator==(a);
	}
};

template<class T1, class T2>
inline bool operator== (const offset_ptr<T1> &pt1, 
                        const offset_ptr<T2> &pt2)
{
	return pt1.get() == pt2.get();
}

#if 0 // TODO:
template<class T1, class T2>
inline bool operator!= (const offset_ptr<T1> &pt1, 
                        const offset_ptr<T2> &pt2)
{  return pt1.get() != pt2.get();  }

//!offset_ptr<T1> < offset_ptr<T2>.
//!Never throws.
template<class T1, class T2>
inline bool operator< (const offset_ptr<T1> &pt1, 
                       const offset_ptr<T2> &pt2)
{  return pt1.get() < pt2.get();  }

//!offset_ptr<T1> <= offset_ptr<T2>.
//!Never throws.
template<class T1, class T2>
inline bool operator<= (const offset_ptr<T1> &pt1, 
                        const offset_ptr<T2> &pt2)
{  return pt1.get() <= pt2.get();  }

//!offset_ptr<T1> > offset_ptr<T2>.
//!Never throws.
template<class T1, class T2>
inline bool operator> (const offset_ptr<T1> &pt1, 
                       const offset_ptr<T2> &pt2)
{  return pt1.get() > pt2.get();  }

//!offset_ptr<T1> >= offset_ptr<T2>.
//!Never throws.
template<class T1, class T2>
inline bool operator>= (const offset_ptr<T1> &pt1, 
                        const offset_ptr<T2> &pt2)
{  return pt1.get() >= pt2.get();  }

//!operator<<
//!for offset ptr
template<class E, class T, class Y> 
inline std::basic_ostream<E, T> & operator<< 
   (std::basic_ostream<E, T> & os, offset_ptr<Y> const & p)
{  return os << p.get_offset();   }

//!operator>> 
//!for offset ptr
template<class E, class T, class Y> 
inline std::basic_istream<E, T> & operator>> 
   (std::basic_istream<E, T> & is, offset_ptr<Y> & p)
{  return is >> p.get_offset();  }

//!std::ptrdiff_t + offset_ptr
//!operation
template<class T>
inline offset_ptr<T> operator+(std::ptrdiff_t diff, const offset_ptr<T>& right)
{  return right + diff;  }

//!offset_ptr - offset_ptr
//!operation
template<class T, class T2>
inline std::ptrdiff_t operator- (const offset_ptr<T> &pt, const offset_ptr<T2> &pt2)
{  return pt.get()- pt2.get();   }
#endif

class simple_shared_memory_manager
{

public:
	enum {
		// Set align to 4096 for vm page align, or 64 for cache-line align.
		// It could be a template parameter.
		align = 64
	};

	inline uint32_t get_size() const
	{
		return size_ * align;
	}

	void reset(uint32_t size)
	{
		size /= align;
		size_ = size;
		next_ = sizeof(simple_shared_memory_manager) / align;
        next_end_ = size;
		named_block_count_ = 0;
	}

	void *allocate(size_t size)
	{
		size = (size + (align -1)) / align;

		size_t pos = next_;
		size_t new_pos = pos + size;

		if (new_pos <= next_end_)
		{
			next_ = (uint32_t)new_pos;
			return ((char *)this + (pos * align));
		}

		out_of_memory_exception e;
		throw e;
	}

	void *allocate_end(size_t size)
	{
		size = (size + (align -1)) / align;

		size_t pos = next_end_;
		size_t new_pos = pos - size;

		if (new_pos >= next_)
		{
			next_end_ = (uint32_t)new_pos;
			return ((char *)this + (new_pos * align));
		}

		out_of_memory_exception e;
		throw e;
	}

	void *create_named_block(int32_t name, int32_t size)
	{
		if (named_block_count_ != 14)
		{
			void *p = allocate(size);
            store_named_block_name_and_pos(name, p);
			return p;
		}
		return 0;
	}

	void *create_named_block_end(int32_t name, int32_t size)
	{
		if (named_block_count_ != 14)
		{
			void *p = allocate_end(size);
            store_named_block_name_and_pos(name, p);
			return p;
		}
		return 0;
	}

	void *find_named_block(int32_t name)
	{
		uint32_t named_block_count = named_block_count_;
		int32_t *pname = (int32_t *)named_block_data_;

		while (named_block_count--)
		{
			if (*pname == name)
			{
				uint32_t offset = *((uint32_t *)pname + 1);
				return ((char *)this + offset);
			}
			pname += 2;
		}

		return 0;
	}

private:
	uint32_t size_;
	uint32_t next_;
	uint32_t next_end_;
	uint32_t named_block_count_;
	struct
	{
		int32_t name;
		uint32_t offset;
	} named_block_data_[14];

	void store_named_block_name_and_pos(int32_t name, void *p)
	{
		int32_t *pname =
			(int32_t *)named_block_data_ + (named_block_count_ * 2);
		*pname = name;
		uint32_t *poffset = ((uint32_t *)pname + 1);
		*poffset = (uint32_t)((char *)p - (char *)this);
		named_block_count_++;
	}
};

template <typename T>
class simple_shared_memory_allocator
{

public:
    typedef T value_type;
    typedef offset_ptr<T> pointer;
    typedef const offset_ptr<T> const_pointer;
    typedef T& reference;
    typedef const T& const_reference;
    typedef uint64_t size_type;
    typedef int64_t difference_type;

public : 
    template<typename U>
    struct rebind
	{
        typedef simple_shared_memory_allocator<U> other;
    };

public : 
	inline explicit simple_shared_memory_allocator(void *mem)
	{
		pshared_memory_manager_ = new (mem) simple_shared_memory_manager;
	}

	inline ~simple_shared_memory_allocator() { }
	
	inline explicit simple_shared_memory_allocator(simple_shared_memory_allocator const& source)
	{
		pshared_memory_manager_ = source.pshared_memory_manager_;
	}
	
	template<typename U>
	inline /* explicit */ simple_shared_memory_allocator(simple_shared_memory_allocator<U> const& source)
	{
		pshared_memory_manager_ = source.pshared_memory_manager_;
	}

	inline pointer address(reference r) { return &r; }

	inline const_pointer address(const_reference r) { return &r; }

public:
	inline pointer allocate(size_type cnt, typename std::allocator<void>::const_pointer = 0)
	{
		return pshared_memory_manager_->allocate(cnt * sizeof(T));
	}

	inline void deallocate(pointer p, size_type)
	{
		not_supported_exception e;
		throw e;
	}

	inline size_type max_size() const
	{ 
		return (pshared_memory_manager_->get_size()) / sizeof(T);
	}

	inline void construct(pointer p, const T& t)
	{
		new(p) T(t);
	}

	inline void destroy(pointer p)
	{
		p->~T();
	}

	inline bool operator == (simple_shared_memory_allocator const& a)
	{
		return a->pshared_memory_manager_ == this->pshared_memory_manager_;
	}

	inline bool operator!=(simple_shared_memory_allocator const& a)
	{
		return !operator==(a);
	}

private:
	offset_ptr<simple_shared_memory_manager> pshared_memory_manager_;

	friend class simple_shared_memory_allocator;
};

class mapped_region;

class shared_memory_object
{

public:
	class create { };
	class open { };
	
	enum mapping {
		memory_mapped,
		file_mapped
	};
	
public:
	shared_memory_object()
	: handle_(0), file_(INVALID_HANDLE_VALUE) {}
	
	shared_memory_object(const char *name, uint32_t size, bool is_system, create,
	mapping mapping = memory_mapped, const char* db_data_dir_path = 0) {
		init_create(name, size, is_system, mapping, db_data_dir_path);
	}

	shared_memory_object(const char *name, open a)
	{
		init_open(name);
	}

	~shared_memory_object();

	inline bool is_valid() const
	{
		return handle_ != 0;
	}

	void init_create(const char *name, uint32_t size, bool is_system, 
	mapping mapping = memory_mapped, const char* db_data_dir_path = 0);
	
	void init_open(const char *name);

private:
	void *handle_;
	HANDLE file_;
	
	friend class mapped_region;
};

class mapped_region
{

public:
	mapped_region()
	{
		address_ = 0;
	}
	
	mapped_region(shared_memory_object &obj)
	{
		init(obj);
	}

	~mapped_region();

	inline bool is_valid() const
	{
		return address_ != 0;
	}

	inline void *get_address() const
	{
		return address_;
	}

	void init(shared_memory_object &obj);

private:
	void *address_;
};

}
}
