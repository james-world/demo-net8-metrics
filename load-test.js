// this is a K6 script
// run with docker from the root of the project:
//   docker run --name k6 -v ${PWD}/load-test.js:/load-test.js --rm grafana/k6 run --insecure-skip-tls-verify --vus 10 -d 1m /load-test.js

import http from 'k6/http';
import exec from 'k6/execution';
import { sleep } from 'k6';

var names = ['james', 'tom', 'pete'];

export default function() {
  // get a random name and roll the dice
  var name = names[(exec.vu.idInTest - 1) % 3];
  http.get(`https://host.docker.internal:8080/rolldice/${name}`);

  // think about it for a bit
  var think_time = Math.random() * (0.9) + 0.1;
  sleep(think_time);
  
  // sometimes call a missing endpoint
  if (Math.random() < 0.1)
    http.get(`https://host.docker.internal:8080/rollsdice/${name}`);
  // sometimes call a broken endpoint
  else if (Math.random() < 0.05)
    http.get(`https://host.docker.internal:8080/explode`);

}
