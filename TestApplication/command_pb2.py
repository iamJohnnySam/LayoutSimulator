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




DESCRIPTOR = _descriptor_pool.Default().AddSerializedFile(b'\n\rcommand.proto\x12\x0eLayoutCommands\"\x80\x02\n\x03Job\x12,\n\x06\x61\x63tion\x18\x01 \x01(\x0e\x32\x1c.LayoutCommands.CommandTypes\x12\x15\n\rtransactionID\x18\x02 \x01(\t\x12\x0e\n\x06target\x18\x03 \x01(\t\x12\x13\n\x0b\x65ndEffector\x18\x04 \x01(\x05\x12\x0c\n\x04slot\x18\x05 \x01(\x05\x12\r\n\x05podID\x18\x06 \x01(\t\x12\r\n\x05state\x18\x07 \x01(\x08\x12\x10\n\x08\x63\x61pacity\x18\x08 \x01(\x05\x12\x13\n\x0bpayloadType\x18\t \x01(\t\x12\x15\n\rtargetStation\x18\n \x01(\t\x12\x11\n\trawAction\x18\x0b \x01(\t\x12\x12\n\nrawCommand\x18\x0c \x01(\t\"U\n\x0c\x43ommandReply\x12\x33\n\x0cresponseType\x18\x01 \x01(\x0e\x32\x1d.LayoutCommands.ResponseTypes\x12\x10\n\x08response\x18\x02 \x01(\t*\xe1\x02\n\x0c\x43ommandTypes\x12\x08\n\x04Pick\x10\x00\x12\t\n\x05Place\x10\x01\x12\x08\n\x04\x44oor\x10\x02\x12\x0c\n\x08\x44oorOpen\x10\x03\x12\r\n\tDoorClose\x10\x04\x12\x07\n\x03Map\x10\x05\x12\x08\n\x04\x44ock\x10\x06\x12\t\n\x05SDock\x10\x07\x12\n\n\x06Undock\x10\x08\x12\x0c\n\x08Process0\x10\t\x12\x0c\n\x08Process1\x10\n\x12\x0c\n\x08Process2\x10\x0b\x12\x0c\n\x08Process3\x10\x0c\x12\x0c\n\x08Process4\x10\r\x12\x0c\n\x08Process5\x10\x0e\x12\x0c\n\x08Process6\x10\x0f\x12\x0c\n\x08Process7\x10\x10\x12\x0c\n\x08Process8\x10\x11\x12\x0c\n\x08Process9\x10\x12\x12\t\n\x05Power\x10\x13\x12\x0b\n\x07PowerOn\x10\x14\x12\x0c\n\x08PowerOff\x10\x15\x12\x08\n\x04Home\x10\x16\x12\x0c\n\x08ReadSlot\x10\x17\x12\x0b\n\x07ReadPod\x10\x18\x12\x07\n\x03Pod\x10\x19\x12\x0b\n\x07Payload\x10\x1a*:\n\rResponseTypes\x12\x07\n\x03\x41\x63k\x10\x00\x12\x08\n\x04Nack\x10\x01\x12\x0b\n\x07Success\x10\x02\x12\t\n\x05\x45rror\x10\x03\x32Y\n\x0fLayoutSimulator\x12\x46\n\x11\x45xecuteSimCommand\x12\x13.LayoutCommands.Job\x1a\x1c.LayoutCommands.CommandReplyb\x06proto3')

_globals = globals()
_builder.BuildMessageAndEnumDescriptors(DESCRIPTOR, _globals)
_builder.BuildTopDescriptorsAndMessages(DESCRIPTOR, 'command_pb2', _globals)
if not _descriptor._USE_C_DESCRIPTORS:
  DESCRIPTOR._loaded_options = None
  _globals['_COMMANDTYPES']._serialized_start=380
  _globals['_COMMANDTYPES']._serialized_end=733
  _globals['_RESPONSETYPES']._serialized_start=735
  _globals['_RESPONSETYPES']._serialized_end=793
  _globals['_JOB']._serialized_start=34
  _globals['_JOB']._serialized_end=290
  _globals['_COMMANDREPLY']._serialized_start=292
  _globals['_COMMANDREPLY']._serialized_end=377
  _globals['_LAYOUTSIMULATOR']._serialized_start=795
  _globals['_LAYOUTSIMULATOR']._serialized_end=884
# @@protoc_insertion_point(module_scope)
