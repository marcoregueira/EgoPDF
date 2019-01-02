import { Component, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';

@Component({
  selector: 'app-fetch-data',
  templateUrl: './fetch-data.component.html'
})
export class FetchDataComponent {

  constructor(
    private http: HttpClient,
    @Inject('BASE_URL') private baseUrl: string) {
  }

  download($event: MouseEvent) {
    debugger;
    $event.cancelBubble = true;
    $event.preventDefault();

    let target = $event.target as HTMLElement;
    let url = <string>target.attributes['href'];
    let name = url.split('/').reverse()[0];
    const headers = new HttpHeaders();
    this.http.get(url, { headers, responseType: 'blob' as 'json' }).subscribe(
      (response: any) => {
        let dataType = response.type;
        let binaryData = [];
        binaryData.push(response);
        let downloadLink = document.createElement('a');
        downloadLink.href = window.URL.createObjectURL(new Blob(binaryData, { type: dataType }));
        if (name)
          downloadLink.setAttribute('download', name);
        document.body.appendChild(downloadLink);
        downloadLink.click();
        downloadLink.parentNode.removeChild(downloadLink);
      }
    )
  }
}
