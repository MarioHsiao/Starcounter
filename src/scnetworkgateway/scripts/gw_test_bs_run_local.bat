:: Setting same environment variables as on the build server.
SET SC_RUNNING_ON_BUILD_SERVER=True
SET SC_RUN_GATEWAY_TESTS=True
SET SC_RUN_TESTS_LOCALLY=True

:: Starting main test script.
gw_test_bs_run_all.bat