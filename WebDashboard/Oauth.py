# import requests
# 
# from Data import config
# 
# 
# class Oauth:
#     @staticmethod
#     def get_access_token(code):
#         payload = {
#             "client_id": config.client_id,
#             "client_secret": config.client_secret,
#             "grant_type": "authorization_code",
#             "code": code,
#             "redirect_uri": config.redirect_uri,
#             "scope": config.scope
#         }
#         headers = {
#             "Content-Type": "application/x-www-form-urlencoded"
#         }
#         # access_token = requests.post(url=config.discord_token_url, data=payload).json()
#         access_token = requests.post('%s/oauth2/token' % config.discord_api_endpoint, data=payload,
#         headers=headers).json()
#         return access_token.get("access_token")
# 
#     @staticmethod
#     def get_user_json(access_token):
#         print(access_token)
#         url = f"{config.discord_api_url}/users/@me"
#         headers = {"Authorization": f"Bearer {access_token}"}
#         user_object = requests.get(url=url, headers=headers).json()
#         print(user_object)  # unauthorised
#         return user_object
