# Running Load Tests

```
docker run --name k6 -v ${PWD}/load-test.js:/load-test.js --rm grafana/k6 run --insecure-skip-tls-verify -vus 10 -d 1m /load-test.js
```
