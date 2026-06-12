#!/usr/bin/env bash
#
# Fires 5 sample FIX messages at the fix-ingress POST /fix endpoint,
# one per second. The endpoint expects: {"message": "<fix string>"}
#
# Usage: ./test_endpoint.sh [host:port]
#   defaults to localhost:8080

set -euo pipefail

HOST="${1:-localhost:8080}"
URL="http://${HOST}/fix"

# Realistic FIX 4.2 messages. The SOH delimiter is rendered as '|' so the
# strings stay JSON-safe; tags: 35=MsgType (D=NewOrderSingle, 8=ExecReport),
# 54=Side (1=Buy,2=Sell), 38=OrderQty, 44=Price, 55=Symbol.
messages=(
  "8=FIX.4.2|9=145|35=D|49=BUYSIDE|56=SELLSIDE|34=1|52=20260612-13:45:01|11=ORD10001|21=1|55=AAPL|54=1|38=100|40=2|44=192.55|59=0|10=128|"
  "8=FIX.4.2|9=152|35=D|49=HEDGEFUND|56=PRIMEBRK|34=2|52=20260612-13:45:02|11=ORD10002|21=1|55=MSFT|54=2|38=250|40=2|44=421.10|59=1|10=097|"
  "8=FIX.4.2|9=160|35=8|49=SELLSIDE|56=BUYSIDE|34=3|52=20260612-13:45:03|37=EXE55001|11=ORD10001|17=FILL001|150=2|39=2|55=AAPL|54=1|38=100|14=100|6=192.55|10=201|"
  "8=FIX.4.2|9=148|35=D|49=ALGODESK|56=SELLSIDE|34=4|52=20260612-13:45:04|11=ORD10003|21=3|55=TSLA|54=1|38=75|40=1|59=3|10=144|"
  "8=FIX.4.2|9=158|35=8|49=SELLSIDE|56=HEDGEFUND|34=5|52=20260612-13:45:05|37=EXE55002|11=ORD10002|17=FILL002|150=1|39=1|55=MSFT|54=2|38=250|14=120|6=421.08|10=176|"
)

echo "Firing ${#messages[@]} FIX messages at ${URL} (1s interval)..."
echo

i=1
for msg in "${messages[@]}"; do
  echo "--- [${i}/${#messages[@]}] POST /fix ---"
  body=$(printf '{"message": "%s"}' "$msg")

  curl -sS -w '\nHTTP %{http_code}  (%{time_total}s)\n' \
    -X POST "$URL" \
    -H 'Content-Type: application/json' \
    -d "$body"

  echo
  i=$((i + 1))
  if [[ $i -le ${#messages[@]} ]]; then
    sleep 1
  fi
done

echo "Done."
