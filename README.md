# Saga Nedir?
Saga, farklı ve bağımsız servisler üzerinde birden fazla transaction’ın sistematik bir şekilde işlenerek veri tutarlılığının sağlanmasını ifade eden ve ilk olarak 1987 yılında akademik bir makalede (bknz: [SAGAS](https://www.cs.cornell.edu/andru/cs711/2002fa/reading/sagas.pdf)) yer edinen bir design pattern’dır. Bu pattern’ın işlevsel mantığı ilk transaction dış bir etki ile(örneğin kullanıcı tarafından butona tıklama) tetiklendikten sonra, bir önceki servisteki transaction’ın başarılı olması durumuna göre bir sonraki servisteki transaction’ın tetiklenmesi ve bir silsile halinde bu şekilde diğer servisler üzerinde tetiklenmenin devam etmesi üzerinedir. Böylece geliştirilen proje her ne kadar microservice yaklaşımını benimsemiş olsa da yapılan bir operasyon neticesinde tüm servislerdeki datalar tutarlı bir şekilde işlenmiş olacaktır.

Tabi süreçte transaction’lardan herhangi birinde oluşabilecek bir hata veya iş mantığı gereği iptal edilmesi gereken bir adım söz konusu olursa tüm süreç iptal edilecek ve işlenen transaction’lar tüm servislerde geri alınacaktır(Compensable Transaction) Böylece Atomicity prensibi desteklenmiş olacaktır.

- ***Saga, microservice’ler arasında doğru transaction yönetimi ve veri tutarlılığı sağlayan bir design pattern’dır.*** 

Saga’nın uygulanması için teknik olarak **Events/Choreography** ve **Command/Orchestration** isminde iki farklı implemantasyon geliştirilmiştir. Şimdi gelin bu implemantasyonları teorik olarak detaylıca inceleyelim.


## Saga – Events/Choreography Implemantasyonu

![image](https://github.com/user-attachments/assets/44693233-c07f-4952-ab39-190201c87edf)

Bu implemantasyonda microservice’ler arası merkezi bir denetim ve haberleşme noktası olmaksızın birbirleriyle eventler aracılığıyla haberleşilmesi esastır. Yani servisler arası iletişimin asenkron olarak tasarlanması gerektiğini savunmaktadır.

Genellikle bu iletişimin message broker ile gerçekleştirilmesi tercih edilir. Ama bu senkron bir iletişiminde kesin olamayacağı anlamına gelmemekte sadece ideal olarak eventler sayesinde asenkron tercih edilmektedir.

Taktiksel olarak Choreography’u incelersek eğer, ilk serviste başlayan transaction işlevini bitirdikten sonra sonraki servise haber gönderebilmek için message broker üzerinden bir event fırlatacaktır. Ardından bu servisteki transaction işlevini bitirdikten sonra kendisinden sonraki servisi tetikleyebilmek için yine bir event fırlatacaktır. Ve bu süreç client’ın istediği işlem bitene kadar silsile halinde işlevdeki son servise kadar devam edecektir. Buradan anlayacağımız her bir servisin kendisinden sonraki servisin tetiklenip tetiklenmeyeceğine dair kararı verdiğini görmekteyiz. Her bir servis yapılan işlev neticesinde kendi sürecine bağlı bir şekilde başarılı ya da başarısız bir karar vermekte ve bu neticeye göre ya kendisinden sonraki servisteki transaction’ın başlamasını sağlayabilmekte ya da tüm transaction’ları geri alabilmektedir. Yani her bir servis bizzat karar verici konumundadır.

- ***Choreography implemantasyonu, distributed transaction’a katılacak olan microservice sayısının 2 ile 4 arasında olduğu durumlarda tercih edilir. 4’ten fazla servis durumunda yazımızın devamında inceleyeceğimiz Orchestration implemantasyonunu uygulamak daha uygundur..***


Choreography’de her bir servis kuyruğu dinler. Dinlediği event message türüne göre gelen bir message söz konusuysa gerekli işlemlerini gerçekleştirir ve sonuç olarak muhakkak durumu bildiren başarılı yahut başarısız bilgisini kuyruğa event olarak ekler. Ardından diğer servisler bu event’e göre ya işlevlerine devam edecektirler ya da tüm transaction’lar geri alınıp veri tutarlılığını sağlayacaktırlar.

Örnek olarak aşağıdaki görseli incelersek eğer bir e-ticaret uygulamasında Choreography implemantasyonuyla oluşturulan siparişi görmekteyiz.

![image](https://github.com/user-attachments/assets/db7bee71-fa44-4c22-9177-c5cf30f1e5d4)

Bu uygulamada bir siparişi oluşturabilmek için;

1. **Order** serviste alınan POST isteği neticesinde sipariş oluşturulur.

2. Order events channel kuyruğuna ‘OrderCreated’ misali bir event gönderilir.

3. **Customer** servisi ise Order events channel‘da ki ‘OrderCreated’ event’ına subscribe olmaktadır. Haliyle ilgili kuyruğa beklenen türde bir event gelirse Customer servis tetiklenecektir.

4. **Customer** servis müşteriye dair gerekli tüm işlemleri yaptıktan sonra eğer işlemler başarılıysa ‘OrderSucceded’ isminde yok eğer değilse ‘CreditLimitExceeded’ isminde Customer evens channel kuyruğuna bir event yayar.

5. Haliyle ilgili kuyruğu dinleyen(subscribe) **Order** servis gelen event’ın türüne göre ya siparişi onaylar ya da reddeder.

Görüldüğü üzere servisler arası uçtan uca(point to point) bir iletişim olmadığı için coupling azalacaktır. Ayrıca transaction yönetimi merkezi olmadığı için performance bottleneck azalacaktır..

Ayrıca Choreography yöntemi, sorumlulukları Saga katılımcı servisleri arasında dağıttığından dolayı tek bir hata noktası olmayacaktır. Bundan dolayı da ekstra bakım gerekmemektedir.

Şimdi Choreography implemantasyonuna daha efektif bir örnek vererek ilerleyelim. Örneğimiz yine bir e-ticaret yazılımının sipariş sürecindeki detaylarını yansıtacaktır;

#### 1. Adım
Kullanıcıdan gelen yeni sipariş isteği neticesinde **Order Service** bu siparişi durum bilgisi **Suspend** olacak şekilde kaydeder. Ardından ödeme işlemlerinin gerçekleştirilebilmesi için ***ORDER_CREATED_EVENT*** isimli event’i fırlatır.

![image](https://github.com/user-attachments/assets/ea544718-2d6c-49be-b347-e16f13fb5c66)

#### 2. Adım
***ORDER_CREATED_EVENT***‘ine subscribe olan **Payment Service** gerekli ödeme işlemlerini gerçekleştirir ve artık alınan ürünlerin stok bilgilerini güncellemek için ***BILLED_ORDER_EVENT*** isimli event’i fırlatır.

#### 3. Adım
***BILLED_ORDER_EVENT***‘ine subscribe olan **Stock Service** bu event fırlatıldığında devreye girer ve gerekli stok ayarlamalarını gerçekleştirir. Artık herhangi bir problem olmadığı taktirde ürünlerin teslimatı için ***ORDER_PREPARED_EVENT*** isimli event’i fırlatır.

#### 4. Adım
***ORDER_PREPARED_EVENT***‘ine subscribe olan **Order Service** artık bu siparişin başarıyla tamamlandığına kanaat getirmiş olarak siparişin durumunu **Completed** olarak günceller.

Yukarıdaki adımlardan herhangi bir durumda hata meydana geldiği taktirde tüm işlemlerin geri alınması gerekmektedir. İşte bu durumda aşağıdaki senaryo devreye girecektir;

![image](https://github.com/user-attachments/assets/3933beef-9b92-4d5f-8a0b-5b2f07d16eb5)

Distributed transaction süreçlerinde bir işlemi geri almak demek esasında o işlemi telafi etmek ya da tam tersini uygulamak için başka bir işlem yapılması demektir(Compensable Transaction) Dolayısıyla yandaki görselden yola çıkarak üstteki işlem akışının yetersiz stok miktarından dolayı **Stock Service**‘de başarısızlığa uğradığını varsayarak sürecin nasıl işlediğini simüle edelim…

#### 1. Adım
Ödeme işleminden sonra **Stock Service**‘te yeterli stok olmadığı anlaşıldığı taktirde ***PRODUCT_OUT_OF_STOCK_EVENT*** isimli event fırlatılır.

#### 2. Adım
***PRODUCT_OUT_OF_STOCK_EVENT***‘ine subscribe olan **Payment Service** ve **Order Service**‘lerde önceki yapılan tüm işlemlerin tersi uygulanır. Nedir bu tersine işlemler diye sorarsanız eğer; **Payment Service**‘te alınan ödeme kullanıcıya tekrar geri yapılır ve **Order Service**‘te ise sipariş durumu Fail olarak güncellenir.


### Saga – Events/Choreography Implemantasyonunun Dezavantajları Nelerdir?

- Hangi servisin hangi kuyruğu dinlediğini takip etmek zorlaşır. Yeni servis eklemek zor ve kafa karıştırıcı olabilmektedir.

- Birbirlerinin kuyruklarını tükettikleri için servisler arası döngüsel bir bağımlılık riski vardır.

- Bir işlemi simüle etmek için tüm servislerin çalışıyor olması gerektiğinden entegrasyon testi zordur.
