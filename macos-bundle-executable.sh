#!/bin/sh

DIR=$(dirname "$0")
ARM64=$(sysctl -ni hw.optional.arm64)

if [[ "$ARM64" == 1 ]]; then
    exec "$DIR/osx-arm64/rabbitcli"
else
    exec "$DIR/osx-x64/rabbitcli"
fi