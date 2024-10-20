# -*- coding: utf-8 -*-
# Generated by the protocol buffer compiler.  DO NOT EDIT!
# NO CHECKED-IN PROTOBUF GENCODE
# source: command.proto
# Protobuf Python Version: 5.27.2
"""Generated protocol buffer code."""
from google.protobuf import descriptor as _descriptor
from google.protobuf import descriptor_pool as _descriptor_pool
from google.protobuf import runtime_version as _runtime_version
from google.protobuf import symbol_database as _symbol_database
from google.protobuf.internal import builder as _builder
_runtime_version.ValidateProtobufRuntimeVersion(
    _runtime_version.Domain.PUBLIC,
    5,
    27,
    2,
    '',
    'command.proto'
)
# @@protoc_insertion_point(imports)

_sym_db = _symbol_database.Default()




DESCRIPTOR = _descriptor_pool.Default().AddSerializedFile(b'\n\rcommand.proto\x12\x0eLayoutCommands\"\xe9\x01\n\x03Job\x12+\n\x06\x61\x63tion\x18\x01 \x01(\x0e\x32\x1b.LayoutCommands.CommandType\x12\x15\n\rtransactionID\x18\x02 \x01(\t\x12\x0e\n\x06target\x18\x03 \x01(\t\x12\x35\n\targuments\x18\x04 \x03(\x0b\x32\".LayoutCommands.Job.ArgumentsEntry\x12\x11\n\trawAction\x18\x05 \x01(\t\x12\x12\n\nrawCommand\x18\x06 \x01(\t\x1a\x30\n\x0e\x41rgumentsEntry\x12\x0b\n\x03key\x18\x01 \x01(\x05\x12\r\n\x05value\x18\x02 \x01(\t:\x02\x38\x01\"U\n\x0c\x43ommandReply\x12\x33\n\x0cresponseType\x18\x01 \x01(\x0e\x32\x1d.LayoutCommands.ResponseTypes\x12\x10\n\x08response\x18\x02 \x01(\t*\x98\x03\n\x0b\x43ommandType\x12\x08\n\x04Pick\x10\x00\x12\t\n\x05Place\x10\x01\x12\x08\n\x04\x44oor\x10\x02\x12\x0c\n\x08\x44oorOpen\x10\x03\x12\r\n\tDoorClose\x10\x04\x12\x07\n\x03Map\x10\x05\x12\x08\n\x04\x44ock\x10\x06\x12\t\n\x05SDock\x10\x07\x12\n\n\x06Undock\x10\x08\x12\x0c\n\x08Process0\x10\t\x12\x0c\n\x08Process1\x10\n\x12\x0c\n\x08Process2\x10\x0b\x12\x0c\n\x08Process3\x10\x0c\x12\x0c\n\x08Process4\x10\r\x12\x0c\n\x08Process5\x10\x0e\x12\x0c\n\x08Process6\x10\x0f\x12\x0c\n\x08Process7\x10\x10\x12\x0c\n\x08Process8\x10\x11\x12\x0c\n\x08Process9\x10\x12\x12\t\n\x05Power\x10\x13\x12\x0b\n\x07PowerOn\x10\x14\x12\x0c\n\x08PowerOff\x10\x15\x12\x08\n\x04Home\x10\x16\x12\x0c\n\x08ReadSlot\x10\x17\x12\x0b\n\x07ReadPod\x10\x18\x12\x07\n\x03Pod\x10\x19\x12\x0b\n\x07Payload\x10\x1a\x12\x0c\n\x08StartSim\x10\x1c\x12\x0b\n\x07StopSim\x10\x1d\x12\x0c\n\x08PauseSim\x10\x1e\x12\r\n\tResumeSim\x10(*\x8e\x01\n\x0e\x43ommandArgType\x12\x0f\n\x0b\x45ndEffector\x10\x00\x12\x08\n\x04Slot\x10\x01\x12\x11\n\rTargetStation\x10\x02\x12\t\n\x05PodID\x10\x03\x12\x0e\n\nDoorStatus\x10\x04\x12\x0f\n\x0bPowerStatus\x10\x05\x12\x0c\n\x08\x43\x61pacity\x10\x06\x12\x08\n\x04Type\x10\x07\x12\n\n\x06Ignore\x10\x08*:\n\rResponseTypes\x12\x07\n\x03\x41\x63k\x10\x00\x12\x08\n\x04Nack\x10\x01\x12\x0b\n\x07Success\x10\x02\x12\t\n\x05\x45rror\x10\x03\x32[\n\x0fLayoutSimulator\x12H\n\x13\x45xecuteCommand_GRPC\x12\x13.LayoutCommands.Job\x1a\x1c.LayoutCommands.CommandReplyb\x06proto3')

_globals = globals()
_builder.BuildMessageAndEnumDescriptors(DESCRIPTOR, _globals)
_builder.BuildTopDescriptorsAndMessages(DESCRIPTOR, 'command_pb2', _globals)
if not _descriptor._USE_C_DESCRIPTORS:
  DESCRIPTOR._loaded_options = None
  _globals['_JOB_ARGUMENTSENTRY']._loaded_options = None
  _globals['_JOB_ARGUMENTSENTRY']._serialized_options = b'8\001'
  _globals['_COMMANDTYPE']._serialized_start=357
  _globals['_COMMANDTYPE']._serialized_end=765
  _globals['_COMMANDARGTYPE']._serialized_start=768
  _globals['_COMMANDARGTYPE']._serialized_end=910
  _globals['_RESPONSETYPES']._serialized_start=912
  _globals['_RESPONSETYPES']._serialized_end=970
  _globals['_JOB']._serialized_start=34
  _globals['_JOB']._serialized_end=267
  _globals['_JOB_ARGUMENTSENTRY']._serialized_start=219
  _globals['_JOB_ARGUMENTSENTRY']._serialized_end=267
  _globals['_COMMANDREPLY']._serialized_start=269
  _globals['_COMMANDREPLY']._serialized_end=354
  _globals['_LAYOUTSIMULATOR']._serialized_start=972
  _globals['_LAYOUTSIMULATOR']._serialized_end=1063
# @@protoc_insertion_point(module_scope)
