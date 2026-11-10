#!/usr/bin/env bash
# Compile the C# scripts against Unity 6.6's engine and editor assemblies, using the
# Roslyn compiler shipped inside the GameCI Unity image. Needs no Unity license.
# Usage: tools/compile_check.sh
set -euo pipefail
IMAGE="${UNITY_IMAGE:-mirror.gcr.io/unityci/editor:ubuntu-6000.6.8f1-base-3}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
docker run --rm -v "$ROOT":/proj -w /proj "$IMAGE" bash -c '
set -e
D=/opt/unity/Editor/Data
# the compiler moved between Unity versions: 6.3 ships DotNetSdkRoslyn, 6.6 a full .NET SDK
if [ -f $D/DotNetSdkRoslyn/csc.dll ]; then
  CSC="$D/NetCoreRuntime/dotnet $D/DotNetSdkRoslyn/csc.dll"
else
  CSC="$D/DotNetSdk/dotnet $(find $D/DotNetSdk/sdk -path "*Roslyn/bincore/csc.dll" | head -1)"
fi
REFS=""
for f in $D/Managed/UnityEngine/*.dll; do REFS="$REFS -r:$f"; done
REFS="$REFS -r:$D/NetStandard/ref/2.1.0/netstandard.dll"
OUT=/tmp/ink; mkdir -p $OUT
RUNTIME=$(find Assets/Scripts -name "*.cs" -not -path "*/Editor/*")
EDITOR=$(find Assets/Scripts -path "*/Editor/*" -name "*.cs")
COMMON="-nologo -noconfig -nostdlib -langversion:9 -target:library -nowarn:1701,1702 -warnaserror- -warn:4"
echo "== runtime (legacy input)"
$CSC $COMMON -define:ENABLE_LEGACY_INPUT_MANAGER -out:$OUT/Runtime.dll $REFS $RUNTIME
echo "== runtime (Input System, as the project builds)"
ISYS=$(find $D/Resources/PackageManager/ProjectTemplates/libcache -name Unity.InputSystem.dll | head -1)
$CSC $COMMON -define:ENABLE_INPUT_SYSTEM -out:$OUT/RuntimeIS.dll $REFS -r:$ISYS $RUNTIME
echo "== editor"
$CSC $COMMON -define:UNITY_EDITOR -define:ENABLE_LEGACY_INPUT_MANAGER -out:$OUT/Editor.dll $REFS -r:$OUT/Runtime.dll $EDITOR
echo "compile check OK"
'
