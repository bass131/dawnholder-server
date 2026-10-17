#!/bin/bash
# The approved sa exception ends at the sqlservr environment; no host env or argv carries it.
set -euo pipefail
set +x
umask 077
fail() { printf '%s\n' 'SQL_BOOTSTRAP_INPUT_REFUSED' >&2; exit 20; }
[[ $# == 0 && $(id -u) == 10001 ]] || fail
[[ -f /bootstrap/sa.txt && ! -L /bootstrap/sa.txt ]] || fail
[[ $(stat -c %s /bootstrap/sa.txt) == 68 ]] || fail
password=$(cat /bootstrap/sa.txt) || fail
[[ $password =~ ^Dh1![0-9A-F]{64}$ ]] || fail
[[ ! -e /run/dawnholder/tls ]] || fail
mkdir /run/dawnholder/tls
cp /bootstrap/server.key /run/dawnholder/tls/server.key
cp /bootstrap/server.pem /run/dawnholder/tls/server.pem
chmod 600 /run/dawnholder/tls/server.key /run/dawnholder/tls/server.pem
# The inherited fixed config points at this private tmpfs; no persistent volume receives a TLS key.
MSSQL_SA_PASSWORD="$password" exec /opt/mssql/bin/sqlservr
