using Starcounter.Advanced;
using Starcounter.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Starcounter {

    internal class ResponseProcTask {

        /// <summary>
        /// Response object.
        /// </summary>
        Response resp_ = null;

        /// <summary>
        /// User delegate.
        /// </summary>
        Action<Response> receiveProc_ = null;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ResponseProcTask(Response resp, Action<Response> receiveProc) {
            resp_ = resp;
            receiveProc_ = receiveProc;
        }

        /// <summary>
        /// Calling the prosedure.
        /// </summary>
        public void CallResponseProc() {
            receiveProc_(resp_);
        }
    };

    internal class AggrTask {

        /// <summary>
        /// Is timed out.
        /// </summary>
        Boolean isTimedOud_;

        /// <summary>
        /// Is task timed out.
        /// </summary>
        internal Boolean TimedOut
        {
            get
            {
                return isTimedOud_;
            }

            set
            {
                isTimedOud_ = value;
            }
        }

        UInt16 aggrTaskIndex_ = UInt16.MaxValue;

        /// <summary>
        /// Linear index of the task when aggregating.
        /// </summary>
        internal UInt16 AggrTaskIndex
        {
            get
            {
                return aggrTaskIndex_;
            }
        }

        UInt16 taskUniqueSalt_ = 0;

        /// <summary>
        /// Unique aggregation salt.
        /// </summary>
        internal UInt16 TaskUniqueSalt
        {
            get
            {
                return taskUniqueSalt_;
            }
        }

        Int32 createdTime_;

        /// <summary>
        /// The time when this socket was created.
        /// </summary>
        internal Int32 CreatedTime
        {
            get
            {
                return createdTime_;
            }
        }

        /// <summary>
        /// Reset salt.
        /// </summary>
        internal void ResetSalt() {
            taskUniqueSalt_ = 0;
        }

        /// <summary>
        /// Refreshing creation time.
        /// </summary>
        internal void InitializeBeforeSend(AggregationClient aggrClient) {
            taskUniqueSalt_ = aggrClient.IncrementUniqueSalt();
        }

        /// <summary>
        /// User delegate.
        /// </summary>
        Action<Response> receiveProc_ = null;

        /// <summary>
        /// Receive delegate to call.
        /// </summary>
        public Action<Response> ReceiveProc
        {
            get
            {
                return receiveProc_;
            }
        }

        /// <summary>
        /// Request bytes.
        /// </summary>
        Byte[] requestBytes_;

        /// <summary>
        /// Request bytes.
        /// </summary>
        public Byte[] RequestBytes
        {
            get
            {
                return requestBytes_;
            }
        }

        /// <summary>
        /// Size of request in bytes.
        /// </summary>
        Int32 requestBytesLength_;

        /// <summary>
        /// 
        /// </summary>
        public Int32 RequestSize
        {
            get
            {
                return requestBytesLength_;
            }
        }

        /// <summary>
        /// Creates new aggregation task.
        /// </summary>
        /// <param name="requestBytes">Request bytes.</param>
        /// <param name="requestBytesLength">Request size in bytes.</param>
        /// <param name="receiveProc">Receive procedure.</param>
        public void Init(
            AggregationClient aggrClient,
            Byte[] requestBytes,
            Int32 requestBytesLength,
            Action<Response> receiveProc) {

            requestBytes_ = requestBytes;
            requestBytesLength_ = requestBytesLength;
            receiveProc_ = receiveProc;
            createdTime_ = aggrClient.CurrentTime;
            isTimedOud_ = false;
            taskUniqueSalt_ = 0;
        }

        /// <summary>
        /// Initial constructor.
        /// </summary>
        /// <param name="aggrTaskIndex"></param>
        public AggrTask(UInt16 aggrTaskIndex) {
            aggrTaskIndex_ = aggrTaskIndex;
        }

        /// <summary>
        /// Detach buffers when they are not needed (so GC can destroy them).
        /// </summary>
        public void DetachBuffers() {
            requestBytes_ = null;
            requestBytesLength_ = 0;
        }
    }

    public class AggregationClient {

        /// <summary>
        /// Static constructor to automatically initialize REST.
        /// </summary>
        static AggregationClient() {

            // Pre-loading assemblies and setting assembly resolvers.
            HelperFunctions.PreLoadCustomDependencies();

            // Initializes HTTP parser.
            Request.sc_init_http_parser();
        }

        /// <summary>
        /// Returns time.
        /// </summary>
        internal Int32 CurrentTime
        {
            get
            {
                return currentTime_;
            }
        }

        /// <summary>
        /// Size of aggregation blob.
        /// </summary>
        const Int32 AggregationBlobSizeBytes = 4 * 1024 * 1024;

        /// <summary>
        /// Aggregation structure.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        struct AggregationStruct {
            public UInt64 unique_socket_id_;
            public Int32 size_bytes_;
            public UInt32 socket_info_index_;
            public UInt16 unique_aggr_index_;
            public UInt16 unique_aggr_salt_;
            public UInt16 port_number_;
            public Byte msg_type_;
            public Byte msg_flags_;
        }

        /// <summary>
        /// Size in bytes of aggregation structure.
        /// </summary>
        const Int32 AggregationStructSizeBytes = 24;

        /// <summary>
        /// Current sent/received balance.
        /// </summary>
        internal Int64 SentReceivedBalance
        {
            get
            {
                return sentReceivedBalance_;
            }
        }

        /// <summary>
        /// Total number of send requests.
        /// </summary>
        public Int64 NumRequestsSent
        {
            get
            {
                return numRequestsSent_;
            }
        }

        /// <summary>
        /// Total number of responses received.
        /// </summary>
        public Int64 NumResponsesReceived
        {
            get
            {
                return numResponsesReceived_;
            }
        }

        /// <summary>
        /// Updates the unique salt.
        /// </summary>
        internal UInt16 IncrementUniqueSalt() {

            curAggrClientSalt_++;
            if (0 == curAggrClientSalt_)
                curAggrClientSalt_++;

            return curAggrClientSalt_;
        }

        /// <summary>
        /// Time interval seconds.
        /// </summary>
        public const Int32 TimeIntervalSeconds = 5;

        /// <summary>
        /// Minimum number of awaited responses.
        /// </summary>
        public const Int32 MinAwaitedResponses = 128;

        /// <summary>
        /// Maximum number of awaited responses.
        /// </summary>
        public const Int32 MaxAwaitedResponses = 8192;

        /// <summary>
        /// Increases load in percent.
        /// </summary>
        public void IncreaseLoad(Int32 numPercent, Boolean setToMaximum = false) {

            if ((numPercent < 10) || (numPercent > 90) || (0 != numPercent % 10)) {
                throw new ArgumentOutOfRangeException("Load percentage should be changed by multiplier of 10.");
            }

            if (setToMaximum) {
                maxAwaitedResponses_ = MaxAwaitedResponses;
            } else {
                maxAwaitedResponses_ = (Int32) (maxAwaitedResponses_ + maxAwaitedResponses_ * (numPercent / 100.0));
                if (maxAwaitedResponses_ > MaxAwaitedResponses)
                    maxAwaitedResponses_ = MaxAwaitedResponses;
            }
        }

        /// <summary>
        /// Decreases load in percent.
        /// </summary>
        public void DecreaseLoad(Int32 numPercent, Boolean setToMinimum = false) {

            if ((numPercent < 10) || (numPercent > 90) || (0 != numPercent % 10)) {
                throw new ArgumentOutOfRangeException("Load percentage should be changed by multiplier of 10.");
            }

            if (setToMinimum) {
                maxAwaitedResponses_ = MinAwaitedResponses;
            } else {
                maxAwaitedResponses_ = (Int32)(maxAwaitedResponses_ - maxAwaitedResponses_ * (numPercent / 100.0));
                if (maxAwaitedResponses_ < MinAwaitedResponses)
                    maxAwaitedResponses_ = MinAwaitedResponses;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public AggregationClient(
            String hostName,
            UInt16 portNumber,
            UInt16 aggrPortNumber,
            Int32 receiveTimeoutSeconds = TimeIntervalSeconds * 3,
            Int32 maxAwaitedResponses = MaxAwaitedResponses) {

            // Checking if timeout is divisible by 5 seconds.
            if (0 != receiveTimeoutSeconds % TimeIntervalSeconds) {
                throw new ArgumentOutOfRangeException("Receive timeout in seconds should divisible by " + TimeIntervalSeconds);
            }

            receiveTimeoutSeconds_ = receiveTimeoutSeconds;
            endpoint_ = hostName + ":" + portNumber;

            origMaxAwaitedResponses_ = maxAwaitedResponses;
            maxAwaitedResponses_ = maxAwaitedResponses;
            if (maxAwaitedResponses >= UInt16.MaxValue) {
                throw new ArgumentOutOfRangeException("Maximum pending aggregation tasks should be less than " + UInt16.MaxValue);
            }

            aggrSocket_ = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            aggrSocket_.ReceiveTimeout = 1000;

            // Trying to set a SIO_LOOPBACK_FAST_PATH on a TCP socket.
            // NOTE: Tries to configure a TCP socket for lower latency and faster operations on the loopback interface.
            try {
                const int SIO_LOOPBACK_FAST_PATH = (-1744830448);

                Byte[] OptionInValue = BitConverter.GetBytes(1);

                aggrSocket_.IOControl(
                    SIO_LOOPBACK_FAST_PATH,
                    OptionInValue,
                    null);
            } catch {
                // Simply ignoring the error if fast loopback is not supported.
            }

            aggrSocket_.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, 1 << 19);
            aggrSocket_.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 1 << 19);

            aggrSocket_.Connect(hostName, aggrPortNumber);

            aggregateSendBlob_ = new Byte[AggregationBlobSizeBytes];
            aggrReceiveBlob_ = new Byte[AggregationBlobSizeBytes];
            receiveAwaitingTasksArray_ = new AggrTask[maxAwaitedResponses_];
            for (UInt16 i = 0; i < maxAwaitedResponses_; i++) {
                AggrTask at = new AggrTask(i);
                freeTasks_.Enqueue(at);
            }

            // Saving the port number in reference aggregation struct.
            referenceAggrStruct_.port_number_ = portNumber;

            unsafe
            {
                fixed (Byte* fixedBytes = aggregateSendBlob_)
                {
                    AggregationStruct* ags = (AggregationStruct*)fixedBytes;
                    *ags = referenceAggrStruct_;
                    ags->size_bytes_ = 0;
                    ags->unique_aggr_index_ = 0;
                    ags->unique_aggr_salt_ = 0;
                    ags->msg_type_ = (Byte)MixedCodeConstants.AggregationMessageTypes.AGGR_CREATE_SOCKET;
                    ags->msg_flags_ = 0;
                }

                aggrSocket_.Send(aggregateSendBlob_, AggregationStructSizeBytes, SocketFlags.None);
                Int32 numBytesReceived = 0;

                while (numBytesReceived != AggregationStructSizeBytes) {

                    numBytesReceived += aggrSocket_.Receive(
                        aggrReceiveBlob_,
                        numBytesReceived,
                        AggregationStructSizeBytes - numBytesReceived,
                        SocketFlags.None);
                }

                fixed (Byte* fixedBytes = aggrReceiveBlob_)
                {
                    AggregationStruct* aggrStruct = (AggregationStruct*)fixedBytes;
                    referenceAggrStruct_ = *aggrStruct;
                }
            }

            // Starting send and receive threads.
            (new Thread(new ThreadStart(AggregateReceiveThread))).Start();
            (new Thread(new ThreadStart(AggregateSendThread))).Start();
            (new Thread(new ThreadStart(ResponsesProcThread))).Start();

            timedOutSocketsTimer_ = new Timer((state) => {
                currentTime_++;
                AggrTask at;

                // Going through all active sockets.
                for (Int32 i = 0; i < receiveAwaitingTasksArray_.Length; i++) {

                    at = receiveAwaitingTasksArray_[i];

                    if ((null != at) && (!at.TimedOut)) {

                        // Checking if socket is in pending state for a long time already.
                        if ((currentTime_ - at.CreatedTime) * TimeIntervalSeconds >= receiveTimeoutSeconds_) {

                            // Setting timed out flag.
                            at.TimedOut = true;

                            // Adding to timeout queue.
                            timedOutTasks_.Enqueue(at);
                        }
                    }
                }

                // Checking tasks that are in the send queue but are not fetched.
                while (tasksToSend_.TryDequeue(out at)) {

                    // Checking if socket is in pending state for a long time already.
                    if ((currentTime_ - at.CreatedTime) * TimeIntervalSeconds >= receiveTimeoutSeconds_) {

                        // Setting timed out flag.
                        at.TimedOut = true;

                        // Calling user delegate with timed out response.
                        Response resp = new Response() {
                            StatusCode = 503,
                            StatusDescription = "Service Unavailable. Time for the response is exceeded."
                        };

                        // Enqueing the receive procedure.
                        responseProcTasks_.Enqueue(new ResponseProcTask(resp, at.ReceiveProc));

                        // Resetting salt.
                        at.ResetSalt();

                        // Releasing free task index.
                        freeTasks_.Enqueue(at);

                    } else {

                        // NOTE: The order can change if we do like this.
                        tasksToSend_.Enqueue(at);
                    }
                }

            }, null, TimeIntervalSeconds * 1000, TimeIntervalSeconds * 1000);
        }

        /// <summary>
        /// Performs aggregation send. 
        /// </summary>
        void AggregateReceiveThread() {

            try {
                Int32 numRecvBytes = 0;

                unsafe
                {
                    fixed (Byte* rb = aggrReceiveBlob_)
                    {
                        while (true) {

                            // Checking if aggregation client is shut down.
                            if (shutdown_)
                                break;

                            // Checking if socket is not connected.
                            if (!aggrSocket_.Connected) {
                                throw new InvalidOperationException("Aggregation socket is not connected.");
                            }

                            // Sleeping so we make less calls to network receive.
                            Thread.Sleep(1);

                            // Checking if we have anything to receive.
                            try {
                                numRecvBytes += aggrSocket_.Receive(
                                    aggrReceiveBlob_,
                                    numRecvBytes,
                                    AggregationBlobSizeBytes - numRecvBytes,
                                    SocketFlags.None);

                            } catch (SocketException e) {

                                // Re-throwing if not a receive timeout.
                                if (e.SocketErrorCode != SocketError.TimedOut) {
                                    throw e;
                                }
                            }

                            // Checking timed out tasks.
                            AggrTask timedOutTask;
                            while (timedOutTasks_.TryDequeue(out timedOutTask)) {

                                // Getting task index and cleaning the slot.
                                UInt16 timedOutTaskIndex = timedOutTask.AggrTaskIndex;
                                UInt16 taskSalt = timedOutTask.TaskUniqueSalt;

                                // Checking if salt is the same.
                                // Salt can be different if receive thread received the task right after it was set to timeout.
                                AggrTask currentTaskUnderIndex = receiveAwaitingTasksArray_[timedOutTaskIndex];

                                if ((currentTaskUnderIndex != null) &&
                                    (0 != currentTaskUnderIndex.TaskUniqueSalt) && 
                                    (taskSalt == currentTaskUnderIndex.TaskUniqueSalt)) {

                                    // Emptying corresponding array element.
                                    receiveAwaitingTasksArray_[timedOutTaskIndex] = null;

                                    // Calling user delegate with timed out response.
                                    Response resp = new Response() {
                                        StatusCode = 503,
                                        StatusDescription = "Service Unavailable. Time for the response is exceeded."
                                    };

                                    // Enqueing the receive procedure.
                                    responseProcTasks_.Enqueue(new ResponseProcTask(resp, timedOutTask.ReceiveProc));

                                    // Resetting salt.
                                    timedOutTask.ResetSalt();

                                    // Releasing free task index.
                                    freeTasks_.Enqueue(timedOutTask);
                                }
                            }

                            // Offset and number of bytes left to process in receive.
                            Int32 processed_bytes_offset = 0, remaining_bytes_to_process = 0;

                            // Processing until we have bytes to process.
                            while (processed_bytes_offset != numRecvBytes) {

                                AggregationStruct* ags = (AggregationStruct*)(rb + processed_bytes_offset);

                                // Checking if message is received completely.
                                Int32 processing_bytes_left = numRecvBytes - processed_bytes_offset;

                                // Checking if we have received the aggregation structure at least.
                                // And if so, checking if we have full message.
                                if ((processing_bytes_left < AggregationStructSizeBytes) ||
                                    (processing_bytes_left < AggregationStructSizeBytes + ags->size_bytes_)) {

                                    // Moving the tail up to the beginning.
                                    Buffer.BlockCopy(aggrReceiveBlob_, processed_bytes_offset, aggrReceiveBlob_, 0, processing_bytes_left);

                                    // Continuing receiving from the beginning.
                                    remaining_bytes_to_process = processing_bytes_left;

                                    // Continue from the beginning.
                                    break;
                                }

                                // Modifying balance.
                                Interlocked.Decrement(ref sentReceivedBalance_);

                                // Incrementing counter.
                                numResponsesReceived_++;

                                // Offset in receive buffer where the response data resides.
                                Int32 responseDataOffset = processed_bytes_offset + AggregationStructSizeBytes;

                                // Switching to next message.
                                processed_bytes_offset += AggregationStructSizeBytes + ags->size_bytes_;

                                // Getting from awaiting task.
                                AggrTask aggrTask = receiveAwaitingTasksArray_[ags->unique_aggr_index_];

                                // Checking if salt is not the same.
                                // This can happen when response comes later than task that was cleaned by timeout.
                                if ((null == aggrTask) ||
                                    (0 == aggrTask.TaskUniqueSalt) ||
                                    (aggrTask.TaskUniqueSalt != ags->unique_aggr_salt_)) {

                                    continue;
                                }

                                // Setting the slot to null.
                                receiveAwaitingTasksArray_[ags->unique_aggr_index_] = null;

                                // Checking type of task.
                                switch ((MixedCodeConstants.AggregationMessageTypes)ags->msg_type_) {

                                    case MixedCodeConstants.AggregationMessageTypes.AGGR_DATA: {

                                        // Constructing the response from received bytes and calling user delegate.
                                        Response resp = new Response(aggrReceiveBlob_, responseDataOffset, ags->size_bytes_, true);

                                        // Enqueing the receive procedure.
                                        responseProcTasks_.Enqueue(new ResponseProcTask(resp, aggrTask.ReceiveProc));

                                        break;
                                    }

                                    case MixedCodeConstants.AggregationMessageTypes.AGGR_CREATE_SOCKET:
                                    case MixedCodeConstants.AggregationMessageTypes.AGGR_DESTROY_SOCKET: {

                                        break;
                                    }
                                }

                                // Resetting salt.
                                aggrTask.ResetSalt();

                                // Releasing free task index.
                                freeTasks_.Enqueue(aggrTask);
                            }

                            // Setting the number of bytes left to be processed.
                            numRecvBytes = remaining_bytes_to_process;
                        }
                    }
                }
            } catch (Exception exc) {
                Console.WriteLine(exc);
                throw exc;
            }
        }

        /// <summary>
        /// Performs aggregation send. 
        /// </summary>
        void AggregateSendThread() {

            try {
                unsafe
                {
                    fixed (Byte* sb = aggregateSendBlob_)
                    {
                        while (true) {

                            // Checking if aggregation client is shut down.
                            if (shutdown_)
                                break;

                            // Checking if socket is not connected.
                            if (!aggrSocket_.Connected) {
                                throw new InvalidOperationException("Aggregation socket is not connected.");
                            }

                            // Only sleeping when there are zero pending tasks.
                            if (tasksToSend_.Count == 0)
                                Thread.Sleep(1);

                            // Checking if we have anything to send.
                            Int32 send_bytes_offset = 0;

                            AggrTask sendTask;

                            // While we have pending tasks to send.
                            while (tasksToSend_.TryDequeue(out sendTask)) {

                                // Getting free task index.
                                UInt16 freeTaskIndex = sendTask.AggrTaskIndex;

                                // Initializing task before sending it.
                                sendTask.InitializeBeforeSend(this);

                                // Putting task to awaiting array.
                                receiveAwaitingTasksArray_[freeTaskIndex] = sendTask;

                                // Incrementing sockets balance.
                                Interlocked.Increment(ref sentReceivedBalance_);

                                // Checking if request fits.
                                if (AggregationStructSizeBytes + sendTask.RequestSize >= AggregationBlobSizeBytes - send_bytes_offset) {

                                    if (0 == send_bytes_offset) {
                                        throw new Exception("Request size is bigger than: " + AggregationBlobSizeBytes);
                                    }

                                    aggrSocket_.Send(aggregateSendBlob_, send_bytes_offset, SocketFlags.None);
                                    send_bytes_offset = 0;
                                }

                                // Creating the aggregation struct.
                                AggregationStruct* ags = (AggregationStruct*)(sb + send_bytes_offset);
                                *ags = referenceAggrStruct_;
                                ags->size_bytes_ = sendTask.RequestSize;
                                ags->unique_aggr_index_ = freeTaskIndex;
                                ags->unique_aggr_salt_ = sendTask.TaskUniqueSalt;
                                ags->msg_type_ = (Byte)MixedCodeConstants.AggregationMessageTypes.AGGR_DATA;
                                ags->msg_flags_ = 0;

                                // Incrementing send statistics.
                                numRequestsSent_++;

                                // Using fast memory copy here.
                                Buffer.BlockCopy(sendTask.RequestBytes, 0, aggregateSendBlob_, send_bytes_offset + AggregationStructSizeBytes, ags->size_bytes_);

                                // Optimization for GC.
                                sendTask.DetachBuffers();

                                // Shifting offset in the array.
                                send_bytes_offset += AggregationStructSizeBytes + ags->size_bytes_;
                            }

                            // Sending last processed requests.
                            if (send_bytes_offset > 0) {
                                aggrSocket_.Send(aggregateSendBlob_, send_bytes_offset, SocketFlags.None);
                            }
                        }
                    }
                }
            } catch (Exception exc) {
                Console.WriteLine(exc);
                throw exc;
            }
        }

        /// <summary>
        /// Sends given request data over aggregation protocol.
        /// </summary>
        /// <param name="reqString">HTTP complete request.</param>
        /// <param name="receiveProc">Receive procedure delegate.</param>
        public void Send(String reqString, Action<Response> receiveProc) {

            Byte[] reqBytes = UTF8Encoding.UTF8.GetBytes(reqString);
            Send(reqBytes, reqBytes.Length, receiveProc);
        }

        /// <summary>
        /// Sends given request data over aggregation protocol.
        /// </summary>
        /// <param name="reqBytes">HTTP complete request.</param>
        /// <param name="reqSize">HTTP request length in bytes.</param>
        /// <param name="receiveProc">Receive procedure delegate.</param>
        public void Send(Byte[] reqBytes, Int32 reqSize, Action<Response> receiveProc) {

            // Checking if aggregation client is shut down.
            if (shutdown_) {
                throw new InvalidOperationException("Aggregation client is already shut down.");
            }

            AggrTask freeTask;

            // Checking the load balance.
            while (origMaxAwaitedResponses_ - maxAwaitedResponses_ != decreasedTasks_.Count) {

                // First checking if we need to fetch from decreased tasks.
                if (origMaxAwaitedResponses_ - maxAwaitedResponses_ < decreasedTasks_.Count) {
                    freeTask = decreasedTasks_.Pop();
                    freeTasks_.Enqueue(freeTask);
                } else {

                    // First we need to obtain a free task.
                    while (!freeTasks_.TryDequeue(out freeTask)) {
                        Thread.Sleep(1);
                    }
                    decreasedTasks_.Push(freeTask);
                }
            }

            // Looping until there are no free task indexes.
            while (!freeTasks_.TryDequeue(out freeTask)) {
                Thread.Sleep(1);
            }

            Debug.Assert(null == receiveAwaitingTasksArray_[freeTask.AggrTaskIndex]);

            // Creating new aggregation task.
            freeTask.Init(this, reqBytes, reqSize, receiveProc);

            // Putting to aggregation queue.
            tasksToSend_.Enqueue(freeTask);
        }

        /// <summary>
        /// Sends given request data over aggregation protocol.
        /// </summary>
        /// <param name="method">HTTP request method.</param>
        /// <param name="uri">HTTP relative URI.</param>
        /// <param name="body">Request body.</param>
        /// <param name="headersDict">Request headers dictionary.</param>
        /// <param name="receiveProc">Receive procedure delegate.</param>
        public void Send(
            String method, 
            String uri, 
            String body, 
            Dictionary<String, String> headersDict, 
            Action<Response> receiveProc) {

            Request req = new Request() {

                Method = method,
                Uri = uri,
                Body = body,
                HeadersDictionary = headersDict,
                Host = endpoint_
            };

            Send(req.RequestBytes, req.RequestLength, receiveProc);
        }

        /// <summary>
        /// Reports statistics from this client.
        /// </summary>
        /// <param name="testName">Test name.</param>
        /// <param name="numOk">Number of successful responses.</param>
        /// <param name="numFailed">Number of failed responses.</param>
        public void SendStatistics(String testName, Int32 numOk, Int32 numFailed) {

            Send("GET", String.Format("/TestStats/AddStats?TestName={0}&NumOk={1}&NumFailed={2}",
                testName, numOk, numFailed), null, null, (Response resp) => {
                    // Do nothing.
            });
        }

        /// <summary>
        /// Shutdown the aggregation client and corresponding threads.
        /// </summary>
        public void Shutdown() {
            shutdown_ = true;
        }

        /// <summary>
        /// Processes received responses. 
        /// </summary>
        void ResponsesProcThread() {

            ResponseProcTask task;

            while (true) {

                // Checking if aggregation client is shut down.
                if (shutdown_)
                    break;

                // While we have pending tasks to send.
                while (responseProcTasks_.TryDequeue(out task)) {
                    task.CallResponseProc();
                }

                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Shutdown flag.
        /// </summary>
        Boolean shutdown_;

        /// <summary>
        /// Bytes blob used for aggregated sends.
        /// </summary>
        Byte[] aggregateSendBlob_;

        /// <summary>
        /// Bytes blob used for aggregated receives.
        /// </summary>
        Byte[] aggrReceiveBlob_;

        /// <summary>
        /// Maximum number of awaited responses.
        /// </summary>
        Int32 maxAwaitedResponses_;

        /// <summary>
        /// Original maximum number of awaited responses.
        /// </summary>
        Int32 origMaxAwaitedResponses_;

        /// <summary>
        /// Endpoint string for this aggregation client.
        /// </summary>
        String endpoint_;

        /// <summary>
        /// Aggregation TCP socket.
        /// </summary>
        Socket aggrSocket_;

        /// <summary>
        /// Aggregation awaiting tasks array.
        /// </summary>
        AggrTask[] receiveAwaitingTasksArray_;

        /// <summary>
        /// Pending async tasks.
        /// </summary>
        ConcurrentQueue<AggrTask> tasksToSend_ = new ConcurrentQueue<AggrTask>();

        /// <summary>
        /// Timed out tasks.
        /// </summary>
        ConcurrentQueue<AggrTask> timedOutTasks_ = new ConcurrentQueue<AggrTask>();

        /// <summary>
        /// Free task indexes.
        /// </summary>
        ConcurrentQueue<AggrTask> freeTasks_ = new ConcurrentQueue<AggrTask>();

        /// <summary>
        /// Tasks to decrease the load.
        /// </summary>
        Stack<AggrTask> decreasedTasks_ = new Stack<AggrTask>();

        /// <summary>
        /// Response processor tasks.
        /// </summary>
        ConcurrentQueue<ResponseProcTask> responseProcTasks_ = new ConcurrentQueue<ResponseProcTask>();

        /// <summary>
        /// Current time.
        /// </summary>
        Int32 currentTime_;

        /// <summary>
        /// Receive timeout in seconds.
        /// </summary>
        Int32 receiveTimeoutSeconds_;

        /// <summary>
        /// Aggregation struct for this aggregation client.
        /// </summary>
        AggregationStruct referenceAggrStruct_;

        /// <summary>
        /// Send/Received balance.
        /// </summary>
        Int64 sentReceivedBalance_ = 0;

        /// <summary>
        /// Send/Received balance.
        /// </summary>
        Int64 numRequestsSent_ = 0;

        /// <summary>
        /// Send/Received balance.
        /// </summary>
        Int64 numResponsesReceived_ = 0;

        /// <summary>
        /// Salt that is uniquely incremented.
        /// </summary>
        UInt16 curAggrClientSalt_;

        // NOTE: Timer should be static, otherwise its garbage collected.
        Timer timedOutSocketsTimer_;
    }
}