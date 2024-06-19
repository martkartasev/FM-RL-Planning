from google.protobuf.internal import containers as _containers
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Iterable as _Iterable, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class IKRequest(_message.Message):
    __slots__ = ("target", "rotation", "currentJoints")
    TARGET_FIELD_NUMBER: _ClassVar[int]
    ROTATION_FIELD_NUMBER: _ClassVar[int]
    CURRENTJOINTS_FIELD_NUMBER: _ClassVar[int]
    target: Vector3
    rotation: Vector3
    currentJoints: _containers.RepeatedScalarFieldContainer[float]
    def __init__(self, target: _Optional[_Union[Vector3, _Mapping]] = ..., rotation: _Optional[_Union[Vector3, _Mapping]] = ..., currentJoints: _Optional[_Iterable[float]] = ...) -> None: ...

class IKResponse(_message.Message):
    __slots__ = ("jointTargets",)
    JOINTTARGETS_FIELD_NUMBER: _ClassVar[int]
    jointTargets: _containers.RepeatedScalarFieldContainer[float]
    def __init__(self, jointTargets: _Optional[_Iterable[float]] = ...) -> None: ...

class Vector3(_message.Message):
    __slots__ = ("x", "y", "z")
    X_FIELD_NUMBER: _ClassVar[int]
    Y_FIELD_NUMBER: _ClassVar[int]
    Z_FIELD_NUMBER: _ClassVar[int]
    x: float
    y: float
    z: float
    def __init__(self, x: _Optional[float] = ..., y: _Optional[float] = ..., z: _Optional[float] = ...) -> None: ...
