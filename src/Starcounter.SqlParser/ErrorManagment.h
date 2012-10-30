///Exception class.
	class UnmanagedParserException {
	public:
		explicit UnmanagedParserException(int err)
		: err_(err) {}
		
		int error_code() const {
			return err_;
		}
		
	private:
		int err_;
	};
