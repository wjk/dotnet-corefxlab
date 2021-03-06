// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Threading.Tasks.Channels.Tests
{
    public class UnbufferedChannelTests : ChannelTestBase
    {
        protected override Channel<int> CreateChannel()
        {
            return Channel.CreateUnbuffered<int>();
        }

        [Fact]
        public async Task Complete_BeforeEmpty_WaitingWriters_TriggersCompletion()
        {
            Channel<int> c = CreateChannel();
            Task write1 = c.Out.WriteAsync(42);
            Task write2 = c.Out.WriteAsync(43);
            c.Out.Complete();
            await c.In.Completion;
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => write1);
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => write2);
        }

        [Fact]
        public void TryReadWrite_NoPartner_Fail()
        {
            Channel<int> c = CreateChannel();
            Assert.False(c.Out.TryWrite(42));
            int result;
            Assert.False(c.In.TryRead(out result));
            Assert.Equal(result, 0);
        }

        [Fact]
        public void ReadAsync_TryWrite_Success()
        {
            Channel<int> c = CreateChannel();
            ValueTask<int> r = c.In.ReadAsync();
            Assert.False(r.IsCompletedSuccessfully);
            Assert.False(r.AsTask().IsCompleted);
            Assert.True(c.Out.TryWrite(42));
            Assert.Equal(42, r.Result);
        }

        [Fact]
        public void TryRead_WriteAsync_Success()
        {
            Channel<int> c = CreateChannel();
            Task w = c.Out.WriteAsync(42);
            Assert.False(w.IsCompleted);
            int result;
            Assert.True(c.In.TryRead(out result));
            Assert.Equal(42, result);
        }

        [Fact]
        public async Task Cancel_UnpartneredWrite_ThrowsCancellationException()
        {
            Channel<int> c = CreateChannel();
            var cts = new CancellationTokenSource();

            Task w = c.Out.WriteAsync(42, cts.Token);
            Assert.False(w.IsCompleted);

            cts.Cancel();
            await AssertCanceled(w, cts.Token);

            ValueTask<int> r = c.In.ReadAsync();
            Assert.False(r.IsCompleted);

            Assert.True(c.Out.TryWrite(42));
            Assert.Equal(42, await r);
        }

        [Fact]
        public async Task Cancel_UnpartneredRead_ThrowsCancellationException()
        {
            Channel<int> c = CreateChannel();
            var cts = new CancellationTokenSource();

            Task r = c.In.ReadAsync(cts.Token).AsTask();
            Assert.False(r.IsCompleted);

            cts.Cancel();
            await AssertCanceled(r, cts.Token);

            Task w = c.Out.WriteAsync(42);
            Assert.False(w.IsCompleted);

            Assert.Equal(42, await c.In.ReadAsync());
            await w;
        }

        [Fact]
        public async Task Cancel_PartneredWrite_Success()
        {
            Channel<int> c = CreateChannel();
            var cts = new CancellationTokenSource();

            Task w = c.Out.WriteAsync(42, cts.Token);
            Assert.False(w.IsCompleted);

            ValueTask<int> r = c.In.ReadAsync();
            Assert.True(r.IsCompletedSuccessfully);

            cts.Cancel();
            await w; // no throw
        }

        [Fact]
        public async Task Cancel_PartneredRead_Success()
        {
            Channel<int> c = CreateChannel();
            var cts = new CancellationTokenSource();

            ValueTask<int> r = c.In.ReadAsync(cts.Token);
            Assert.False(r.IsCompletedSuccessfully);

            Task w = c.Out.WriteAsync(42);
            Assert.True(w.IsCompleted);

            cts.Cancel();
            Assert.Equal(42, await r);
        }

    }
}
