#pragma once
#ifndef PROFILER_HPP
#define PROFILER_HPP

namespace starcounter {
namespace network {

class PreciseTimer
{
    LARGE_INTEGER start_time_large_, stop_time_large_;
    int64_t total_time_, start_count_;
    double freq_;

public:
    PreciseTimer()
    {
        // Calculating frequency.
        LARGE_INTEGER freqTempVar;
        if (QueryPerformanceFrequency(&freqTempVar) == false)
        {
            // High-performance counter not supported.
            GW_COUT << "High-performance counter not supported." << GW_ENDL;
            return;
        }

        // Resetting the timer.
        Reset();

        // Timer frequency indicator.
        freq_ = (double)(freqTempVar.QuadPart);
    }

    // Start the timer.
    void Start()
    {
        start_count_++;
        QueryPerformanceCounter(&start_time_large_);
    }

    // Stop the timer.
    void Stop()
    {
        QueryPerformanceCounter(&stop_time_large_);
        total_time_ += (stop_time_large_.QuadPart - start_time_large_.QuadPart);
    }

    // Reseting all counters.
    void Reset()
    {
        memset(&start_time_large_, 0, sizeof(LARGE_INTEGER));
        memset(&stop_time_large_, 0, sizeof(LARGE_INTEGER));

        total_time_ = 0;
        start_count_ = 0;
    }

    // Returns the number of times the timer was started.
    int64_t start_count()
    {
        return start_count_;
    }

    // Returns the duration of the timer (in milliseconds).
    double DurationMs()
    {
        return (total_time_ / freq_) * 1000.0;
    }
};

class Profiler
{
    // Each timer has a descriptive name.
    std::string *timer_names_;
    PreciseTimer *stopwatches_;

    // Each checkpoint has a descriptive name.
    std::string *checkpoint_names_;
    int64_t *checkpoint_ticks_;

    // Amount of timers in the profiler.
    int32_t timers_num_;

public:

    Profiler()
    {
        timer_names_ = NULL;
        stopwatches_ = NULL;
        checkpoint_names_ = NULL;
        checkpoint_ticks_ = NULL;
        timers_num_ = 0;
    }

    void Init(int32_t maxTimersNumber)
    {
        timers_num_ = maxTimersNumber;
        timer_names_ = new std::string[timers_num_];
        stopwatches_ = new PreciseTimer[timers_num_];

        checkpoint_names_ = new std::string[timers_num_];
        checkpoint_ticks_ = new int64_t[timers_num_];

        for (int32_t i = 0; i < timers_num_; i++)
        {
            timer_names_[i] = "";
            checkpoint_names_[i] = "";
            checkpoint_ticks_[i] = 0;
        }
    }

    /// <summary>
    /// Used to count how many times execution went through this checkpoint.
    /// </summary>
    void Checkpoint(std::string name, int32_t checkpointId) // Example: Application.Profiler.Checkpoint("Number of socket->send calls", 0);
    {
        if (checkpoint_names_[checkpointId].empty()) // Assign description only on the first time.
            checkpoint_names_[checkpointId] = name;

        checkpoint_ticks_[checkpointId]++;
    }

    /// <summary>
    /// Starts specified timer.
    /// </summary>
    void Start(std::string name, int32_t timerId) // Example: Application.Profiler.Start("Time used for matrix multiplication", 0);
    {
        if (timer_names_[timerId].empty()) // Assign description only on the first time.
            timer_names_[timerId] = name;

        stopwatches_[timerId].Start();
    }

    /// <summary>
    /// Stops specified timer.
    /// </summary>
    void Stop(int32_t timerId) // Example: Application.Profiler.Stop(0);
    {
        stopwatches_[timerId].Stop();
    }

    /// <summary>
    /// Gets profiling results when specified timer reaches certain start count.
    /// </summary>
    std::string GetResults(int32_t timerId, int32_t startCount)
    {
        if (stopwatches_[timerId].start_count() >= startCount)
            return GetResults();

        return "No results.";
    }

    /// <summary>
    /// Gets profiling results when specified timer reaches certain start count.
    /// </summary>
    void DrawResults(int32_t timerId, int32_t startCount)
    {
        if (stopwatches_[timerId].start_count() >= startCount)
            std::cout << GetResults();
    }

    /// <summary>
    /// Prints profiler results to the console.
    /// </summary>
    void DrawResults()
    {
        GW_COUT << GetResults() << GW_ENDL;
    }

    /// <summary>
    /// Fetches all timer results and resets all counters.
    /// </summary>
    std::string GetResults()
    {
        // Assuming that all timers stopped at this moment.
        std::stringstream stringStream;

        // Going through all timers.
        stringStream << "==== Profiling results ====" << GW_ENDL;
        for (int32_t i = 0; i < timers_num_; i++)
        {
            if (!timer_names_[i].empty())
            {
                stringStream << "  #" << i << " \"" << timer_names_[i] << "\" took " << stopwatches_[i].DurationMs() << " ms and was started " << stopwatches_[i].start_count() << " times." << GW_ENDL;
                stopwatches_[i].Reset();
            }
        }
        stringStream << "==== End of Profiling results ====" << GW_ENDL;

        // Printing all checkpoints.
        stringStream << "==== Checkpoint results ====" << GW_ENDL;
        for (int32_t i = 0; i < timers_num_; i++)
        {
            if (!checkpoint_names_[i].empty())
            {
                stringStream << "  #" << i << " \"" << checkpoint_names_[i] << "\" was called " << checkpoint_ticks_[i] << " times." << GW_ENDL;
                checkpoint_ticks_[i] = 0;
            }
        }

        stringStream << "==== End of Checkpoint results ====" << GW_ENDL;

        // Outputting to string.
        return stringStream.str();
    }

    /// <summary>
    /// Gets specific timer results and resets timer.
    /// </summary>
    std::string GetSpecific(int32_t timerId)
    {
        // Assuming that timer is stopped at this moment.
        std::stringstream stringStream;
        stringStream << "==== Specific Timer results ====" << GW_ENDL;

        // Checking if timer was running at all.
        if (!timer_names_[timerId].empty())
        {
            stringStream << "  #" << timerId << " \"" << timer_names_[timerId] << "\" took " << stopwatches_[timerId].DurationMs() << " ms and was started " << stopwatches_[timerId].start_count() << " times." << GW_ENDL;
            stopwatches_[timerId].Reset();
        }

        stringStream << "==== End of Timer results ====" << GW_ENDL;

        // Outputting to string.
        return stringStream.str();
    }
};

} // namespace network
} // namespace starcounter

#endif // PROFILER_HPP