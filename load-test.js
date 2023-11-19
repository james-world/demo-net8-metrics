import http from 'k6/http';
import exec from 'k6/execution';
import { sleep } from 'k6';

var names = ['james', 'tom', 'pete'];


export default function() {
  var name = names[(exec.vu.idInTest - 1) % 3];
  http.get(`https://host.docker.internal:8080/rolldice/${name}`);

  var 
  think_time = Math.random() * (0.9) + 0.1;
  sleep(think_time);
}
