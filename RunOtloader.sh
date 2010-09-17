OTLOADERROOT=$(cd "${0%/*}" && echo $PWD)
USRLOCALLIB="/usr/local/lib/"

#determine platform
UNAME=`uname`
if [ "$UNAME" == "Darwin" ]; then
   export DYLD_LIBRARY_PATH="${OTLOADERROOT}":"${USRLOCALLIB}":$DYLD_LIBRARY_PATH
elif [ "$UNAME" == "Linux" ]; then
   export LD_LIBRARY_PATH="${OTLOADERROOT}":"${USRLOCALLIB}":$LD_LIBRARY_PATH
fi

cd "$OTLOADERROOT"
./otloader

