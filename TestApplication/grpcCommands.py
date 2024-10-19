import grpc
import command_pb2
import command_pb2_grpc
import asyncio
from datetime import datetime

def run_commands():

    pod = asyncio.run(execute(command_pb2.Job(action = command_pb2.Pod, capacity=25, payloadType="payload")))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Payload, slot=1)))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Payload, slot=5)))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Payload, slot=23)))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Payload, slot=25)))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Dock, target="L1", podID = str(pod))))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Map, target="L1")))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.PowerOn, target="R1")))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Pick, target="R1", endEffector=1, targetStation="L1", slot=3)))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Pick, target="R1", endEffector=1, targetStation="L1", slot=1)))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Place, target="R1", endEffector=1, targetStation="A1", slot=1)))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Door, target="L1", state=True)))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Map, target="L1")))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Process0, target="A1")))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Pick, target="R1", endEffector=1, targetStation="A1", slot=1)))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.DoorOpen, target="P1")))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Place, target="R1", endEffector=1, targetStation="P1", slot=1)))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.DoorClose, target="P1")))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Process0, target="P1")))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Pick, target="R1", endEffector=1, targetStation="L1", slot=2)))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.DoorOpen, target="P1")))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Pick, target="R1", endEffector=2, targetStation="P1", slot=2)))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Place, target="R1", endEffector=1, targetStation="P1", slot=2)))




async def execute(job):
    async with grpc.aio.insecure_channel('localhost:50051') as channel:
        stub = command_pb2_grpc.LayoutSimulatorStub(channel)

        t = datetime.now()
        print()
        print(f"{t.strftime('%H:%M:%S')} : Command -> {job.action} | Target -> {job.target}.")

        response = await stub.ExecuteCommand_GRPC(job)

        if response.responseType == 0:
            m = "ACK"
        elif response.responseType == 1:
            m = "NACK"
        elif response.responseType == 2:
            m = "SUCCESS"
        else:
            m = "ERROR"

        print(f"    {(datetime.now() - t).total_seconds()}: Response -> {m} | Message -> {response.response}.")
        return response.response