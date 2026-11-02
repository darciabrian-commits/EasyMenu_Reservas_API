import http from 'k6/http';
import { check } from 'k6';

export default function () {
  const url = __ENV.EASYMENU_QA_URL;
  if (!url) throw new Error('Falta EASYMENU_QA_URL');
  const res = http.get(`${url}/api/productos`);
  check(res, {
    'sin error servidor': (r) => r.status < 500,
    'respuesta HTTP 200': (r) => r.status === 200,
  });
}
